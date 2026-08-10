using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Models;
using MidnightApi.Services;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/access-tokens")]
[Tags("AccessTokens")]
[Authorize(Roles = AuthRoles.Admin)]
public class AccessTokenController : ControllerBase
{
    private readonly IAccessTokensRepository _tokens;
    private readonly IFamiliesRepository _families;
    private readonly CommonService _commonService;
    private readonly PasswordService _passwordService;

    public AccessTokenController(
        IAccessTokensRepository tokens,
        IFamiliesRepository families,
        CommonService commonService,
        PasswordService passwordService)
    {
        _tokens = tokens;
        _families = families;
        _commonService = commonService;
        _passwordService = passwordService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var items = await _tokens.GetListAsync(new InputAccessTokenList
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
        _commonService.ValidateModelState(ModelState);

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
        _commonService.ValidateModelState(ModelState);
        ValidateScopeMember(request.Scope, request.MemberId);

        var familyId = User.GetFamilyId();
        var family = await _families.GetByIdAsync(new InputGetFamily { Id = familyId })
            ?? throw new NotFoundException("Family not found.");

        var expiresOn = AccessTokenExpiryHelper.ResolveExpiresOn(request.ExpiryPreset, request.CustomExpiresOn);
        if (expiresOn <= DateTimeOffset.UtcNow)
        {
            throw new BadRequestException("Expiry must be in the future.");
        }

        var rawToken = AccessTokenSecretGenerator.GenerateRawToken(family.FamilyCode);
        var tokenHash = _passwordService.Hash(rawToken);
        var tokenPreview = AccessTokenSecretGenerator.BuildPreview(rawToken);

        var result = await _tokens.CreateAsync(new InputCreateAccessToken
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

        _commonService.EnsureSuccess(result);
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
        _commonService.ValidateModelState(ModelState);
        ValidateScopeMember(request.Scope, request.MemberId);

        _ = await LoadTokenOrThrow(route.Id);

        var expiresOn = AccessTokenExpiryHelper.ResolveExpiresOn(request.ExpiryPreset, request.CustomExpiresOn);
        if (expiresOn <= DateTimeOffset.UtcNow)
        {
            throw new BadRequestException("Expiry must be in the future.");
        }

        var result = await _tokens.UpdateAsync(new InputUpdateAccessToken
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

        _commonService.EnsureSuccess(result);
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
        _commonService.ValidateModelState(ModelState);

        _ = await LoadTokenOrThrow(route.Id);

        var result = await _tokens.SetStatusAsync(new InputSetAccessTokenStatus
        {
            FamilyId = User.GetFamilyId(),
            TokenId = route.Id,
            NewStatus = request.Status,
            UpdatedBy = User.GetUsername()
        });

        _commonService.EnsureSuccess(result);
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
        _commonService.ValidateModelState(ModelState);

        _ = await LoadTokenOrThrow(route.Id);

        var result = await _tokens.SoftDeleteAsync(new InputDeleteAccessToken
        {
            FamilyId = User.GetFamilyId(),
            TokenId = route.Id,
            CancelledBy = User.GetUsername()
        });

        _commonService.EnsureSuccess(result);

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
        return await _tokens.GetByIdAsync(new InputGetAccessToken
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

    private static void ValidateScopeMember(string scope, long? memberId)
    {
        if (NeedsMember(scope) && (memberId is null or <= 0))
        {
            throw new BadRequestException("Select a valid family member for this scope.");
        }
    }
}
