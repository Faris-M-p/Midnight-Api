using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/access-tokens")]
[Tags("AccessTokens")]
[Authorize(Roles = AuthRoles.Admin)]
public class AccessTokenController : ControllerBase
{
    private readonly IAccessTokensRepository _iAccessTokensRepository;
    private readonly IFamiliesRepository _iFamiliesRepository;
    private readonly ICommonService _iCommonService;
    private readonly IPasswordService _iPasswordService;
    private readonly IJwtTokenService _iJwtTokenService;
    private readonly IAccessTokenSecretGenerator _iAccessTokenSecretGenerator;

    public AccessTokenController(
        IAccessTokensRepository tokens,
        IFamiliesRepository families,
        ICommonService commonService,
        IPasswordService passwordService,
        IJwtTokenService jwt,
        IAccessTokenSecretGenerator secrets)
    {
        _iAccessTokensRepository = tokens;
        _iFamiliesRepository = families;
        _iCommonService = commonService;
        _iPasswordService = passwordService;
        _iJwtTokenService = jwt;
        _iAccessTokenSecretGenerator = secrets;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] InputAccessTokenLoginView request)
    {
        _iCommonService.ValidateModelState(ModelState);

        const string invalidMessage = "Invalid family code or access token.";
        var rawToken = request.AccessToken.Trim();

        if (!_iAccessTokenSecretGenerator.TryParse(rawToken, out var familyCodePrefix, out _))
        {
            throw new UnauthorizedAccessException(invalidMessage);
        }

        var family = await _iFamiliesRepository.GetByCodeAsync(new InputGetFamilyByCode { FamilyCode = familyCodePrefix });
        if (family is null
            || !_iAccessTokenSecretGenerator.MatchesFamilyPrefix(rawToken, family.FamilyCode))
        {
            throw new UnauthorizedAccessException(invalidMessage);
        }

        var candidates = await _iAccessTokensRepository.ListForLoginAsync(new InputAccessTokenList
        {
            FamilyId = family.Id
        });

        OutputAccessTokenLoginCandidate? matched = null;
        foreach (var candidate in candidates)
        {
            if (_iPasswordService.Verify(rawToken, candidate.TokenHash))
            {
                matched = candidate;
                break;
            }
        }

        if (matched is null)
        {
            throw new UnauthorizedAccessException(invalidMessage);
        }

        if (!IsValidTokenConfiguration(matched))
        {
            throw new UnauthorizedAccessException(invalidMessage);
        }

        if (string.Equals(matched.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("This access token is inactive.");
        }

        if (matched.ExpiresOn <= DateTimeOffset.UtcNow
            || string.Equals(matched.Status, "Expired", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("This access token has expired.");
        }

        if (!string.Equals(matched.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(invalidMessage);
        }

        _ = await _iAccessTokensRepository.RecordLoginAsync(new InputAccessTokenRecordLogin
        {
            FamilyId = family.Id,
            TokenId = matched.Id
        });

        var (jwt, expiresAt) = _iJwtTokenService.CreateAccessTokenSession(
            family.Id,
            matched.Id,
            matched.TokenName,
            matched.Permission,
            matched.Scope,
            matched.MemberId,
            matched.ExpiresOn);

        return Ok(new ApiResponse<OutputAccessTokenLogin>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Login successful.",
            Data = new OutputAccessTokenLogin
            {
                AccessToken = jwt,
                ExpiresAtUtc = expiresAt,
                TokenType = "Bearer",
                User = new OutputAccessTokenLoginUser
                {
                    AuthType = AuthTypes.AccessToken,
                    FamilyId = family.Id,
                    TokenId = matched.Id,
                    TokenName = matched.TokenName,
                    Permission = matched.Permission,
                    Scope = matched.Scope,
                    ScopeMemberId = matched.MemberId,
                    IsAdmin = false
                }
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var items = await _iAccessTokensRepository.GetListAsync(new InputAccessTokenList
        {
            FamilyId = User.GetFamilyId()
        });

        return Ok(new ApiResponse<List<OutputAccessTokenItem>>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = items,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] InputAccessTokenRouteRequestView request)
    {
        _iCommonService.ValidateModelState(ModelState);

        var token = await LoadTokenOrThrow(request.Id);

        return Ok(new ApiResponse<OutputGetAccessToken>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = token,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InputCreateAccessTokenView request)
    {
        _iCommonService.ValidateModelState(ModelState);
        ValidateScopeMember(request.Scope, request.MemberId);

        var familyId = User.GetFamilyId();
        var family = await _iFamiliesRepository.GetByIdAsync(new InputGetFamily { Id = familyId })
            ?? throw new NotFoundException("Family not found.");

        var expiresOn = _iAccessTokenSecretGenerator.ResolveExpiresOn(request.ExpiryPreset, request.CustomExpiresOn);
        if (expiresOn <= DateTimeOffset.UtcNow)
        {
            throw new BadRequestException("Expiry must be in the future.");
        }

        var rawToken = _iAccessTokenSecretGenerator.GenerateRawToken(family.FamilyCode);
        var tokenHash = _iPasswordService.Hash(rawToken);
        var tokenPreview = _iAccessTokenSecretGenerator.BuildPreview(rawToken);

        var result = await _iAccessTokensRepository.CreateAsync(new InputCreateAccessToken
        {
            FamilyId = familyId,
            TokenName = request.TokenName,
            Permission = request.Permission,
            Scope = request.Scope,
            MemberId = NeedsMember(request.Scope) ? request.MemberId : null,
            TokenHash = tokenHash,
            TokenPreview = tokenPreview,
            ExpiresOn = expiresOn,
            CreatedBy = User.GetUsername()
        });

        _iCommonService.EnsureSuccess(result);
        var tokenId = ResolveTokenId(result);
        var token = await LoadTokenOrThrow(tokenId);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputCreateAccessTokenResult>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = result.ResponseMessage ?? "Access token generated successfully.",
            Data = new OutputCreateAccessTokenResult
            {
                Id = tokenId,
                RawToken = rawToken,
                Token = token
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        [FromRoute] InputAccessTokenRouteRequestView route,
        [FromBody] InputUpdateAccessTokenView request)
    {
        _iCommonService.ValidateModelState(ModelState);
        ValidateScopeMember(request.Scope, request.MemberId);

        _ = await LoadTokenOrThrow(route.Id);

        var expiresOn = _iAccessTokenSecretGenerator.ResolveExpiresOn(request.ExpiryPreset, request.CustomExpiresOn);
        if (expiresOn <= DateTimeOffset.UtcNow)
        {
            throw new BadRequestException("Expiry must be in the future.");
        }

        var result = await _iAccessTokensRepository.UpdateAsync(new InputUpdateAccessToken
        {
            FamilyId = User.GetFamilyId(),
            TokenId = route.Id,
            TokenName = request.TokenName,
            Permission = request.Permission,
            Scope = request.Scope,
            MemberId = NeedsMember(request.Scope) ? request.MemberId : null,
            ExpiresOn = expiresOn,
            UpdatedBy = User.GetUsername()
        });

        _iCommonService.EnsureSuccess(result);
        var token = await LoadTokenOrThrow(route.Id);

        return Ok(new ApiResponse<OutputGetAccessToken>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = result.ResponseMessage ?? "Access token updated successfully.",
            Data = token,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> SetStatus(
        [FromRoute] InputAccessTokenRouteRequestView route,
        [FromBody] InputSetAccessTokenStatusView request)
    {
        _iCommonService.ValidateModelState(ModelState);

        _ = await LoadTokenOrThrow(route.Id);

        var result = await _iAccessTokensRepository.SetStatusAsync(new InputSetAccessTokenStatus
        {
            FamilyId = User.GetFamilyId(),
            TokenId = route.Id,
            NewStatus = request.Status,
            UpdatedBy = User.GetUsername()
        });

        _iCommonService.EnsureSuccess(result);
        var token = await LoadTokenOrThrow(route.Id);

        return Ok(new ApiResponse<OutputGetAccessToken>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = result.ResponseMessage ?? "Access token status updated successfully.",
            Data = token,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] InputAccessTokenRouteRequestView route)
    {
        _iCommonService.ValidateModelState(ModelState);

        _ = await LoadTokenOrThrow(route.Id);

        var result = await _iAccessTokensRepository.SoftDeleteAsync(new InputDeleteAccessToken
        {
            FamilyId = User.GetFamilyId(),
            TokenId = route.Id,
            CancelledBy = User.GetUsername()
        });

        _iCommonService.EnsureSuccess(result);

        return Ok(new ApiResponse<object?>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = result.ResponseMessage ?? "Access token deleted successfully.",
            Data = null,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    private async Task<OutputGetAccessToken> LoadTokenOrThrow(long tokenId)
    {
        return await _iAccessTokensRepository.GetByIdAsync(new InputGetAccessToken
        {
            FamilyId = User.GetFamilyId(),
            TokenId = tokenId
        }) ?? throw new NotFoundException("Access token not found.");
    }

    private static long ResolveTokenId(OutputCreateAccessToken result)
    {
        if (result.Data?.Id > 0)
        {
            return result.Data.Id;
        }

        if (result.ResponseCode > 0)
        {
            return result.ResponseCode;
        }

        throw new BadRequestException("Access token was created, but the id could not be resolved.");
    }

    private static bool NeedsMember(string scope) =>
        scope is "SelectedMember" or "MemberDescendants";

    private static bool IsValidTokenConfiguration(OutputAccessTokenLoginCandidate token)
    {
        if (token.Permission is not ("View" or "Edit"))
        {
            return false;
        }

        if (token.Scope is not ("EntireFamily" or "SelectedMember" or "MemberDescendants"))
        {
            return false;
        }

        if (NeedsMember(token.Scope) && token.MemberId is null or <= 0)
        {
            return false;
        }

        return true;
    }

    private static void ValidateScopeMember(string scope, long? memberId)
    {
        if (NeedsMember(scope) && (memberId is null or <= 0))
        {
            throw new BadRequestException("Select a valid family member for this scope.");
        }
    }
}
