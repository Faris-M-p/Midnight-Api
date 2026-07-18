using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Models.Api;
using MidnightApi.Services;

namespace MidnightApi.Controllers;

/// <summary>Admin registration, login, and account profile (one account = one family).</summary>
[ApiController]
[Route("api/accounts")]
[Tags("Accounts")]
public class AccountController : ControllerBase
{
    private readonly IUserAccountsRepository _accounts;
    private readonly IFamiliesRepository _families;
    private readonly PasswordService _passwords;
    private readonly JwtTokenService _jwt;
    private readonly CommonService _commonService;

    public AccountController(
        IUserAccountsRepository accounts,
        IFamiliesRepository families,
        PasswordService passwords,
        JwtTokenService jwt,
        CommonService commonService)
    {
        _accounts = accounts;
        _families = families;
        _passwords = passwords;
        _jwt = jwt;
        _commonService = commonService;
    }

    /// <summary>Register admin account and create its one family.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] InputRegisterAccountView request)
    {
        _commonService.ValidateModelState(ModelState);

        if (await _accounts.ExistsByUsernameAsync(request.Username.Trim()))
        {
            throw new ConflictException("Username already exists.");
        }

        if (await _families.ExistsByCodeAsync(request.FamilyCode.Trim()))
        {
            throw new ConflictException("Family code already exists.");
        }

        var actor = request.Username.Trim();
        var family = await _families.CreateAsync(new InputCreateFamily
        {
            FamilyCode = request.FamilyCode.Trim(),
            FamilyName = request.FamilyName.Trim(),
            Description = request.Description
        }, actor);

        var account = await _accounts.CreateAsync(new InputCreateAccount
        {
            FK_Families = family.ID_Families,
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = _passwords.Hash(request.Password),
            IsActive = true
        }, actor);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputRegister>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = "Account registered successfully.",
            Data = new OutputRegister
            {
                AccountId = account.ID_UserAccounts,
                FamilyId = family.ID_Families,
                Username = account.Username
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>Admin login. Returns JWT with FamilyId claim.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] InputLoginView request)
    {
        _commonService.ValidateModelState(ModelState);

        var login = await _accounts.GetLoginByUsernameAsync(request.Username.Trim());
        if (login is null || !_passwords.Verify(request.Password, login.Value.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var account = login.Value.Account;
        if (!account.IsActive)
        {
            throw new UnauthorizedAccessException("Account is disabled.");
        }

        if (await _families.GetByIdAsync(account.FK_Families) is null)
        {
            throw new NotFoundException("Family linked to this account was not found.");
        }

        var (token, expiresAt) = _jwt.CreateAdminToken(
            account.ID_UserAccounts,
            account.FK_Families,
            account.Username);

        return Ok(new ApiResponse<OutputLogin>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Login successful.",
            Data = new OutputLogin
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAt
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>Get the authenticated admin account.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        _commonService.ValidateModelState(ModelState);

        var account = await _accounts.GetByIdAsync(User.GetAccountId())
            ?? throw new NotFoundException("Account not found.");

        return Ok(new ApiResponse<OutputGetAccount>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = account,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>Update the authenticated admin account profile.</summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] InputUpdateAccountView request)
    {
        _commonService.ValidateModelState(ModelState);

        var accountId = User.GetAccountId();
        if (await _accounts.ExistsByUsernameAsync(request.Username.Trim(), accountId))
        {
            throw new ConflictException("Username already exists.");
        }

        var updated = await _accounts.UpdateAsync(accountId, new InputUpdateAccount
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = string.IsNullOrWhiteSpace(request.Password) ? null : _passwords.Hash(request.Password)
        }, User.GetUsername())
            ?? throw new NotFoundException("Account not found.");

        return Ok(new ApiResponse<OutputGetAccount>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Account updated successfully.",
            Data = updated,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
