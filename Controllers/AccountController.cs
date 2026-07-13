using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Services;

namespace MidnightApi.Controllers;

using MidnightApi.Models.Api;

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
    private readonly MemberValidationService _validation;

    public AccountController(
        IUserAccountsRepository accounts,
        IFamiliesRepository families,
        PasswordService passwords,
        JwtTokenService jwt,
        MemberValidationService validation)
    {
        _accounts = accounts;
        _families = families;
        _passwords = passwords;
        _jwt = jwt;
        _validation = validation;
    }

    /// <summary>Register admin account and create its one family.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] InputRegisterAccountView request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException("Username and password are required.");
        }

        if (string.IsNullOrWhiteSpace(request.FamilyCode) || string.IsNullOrWhiteSpace(request.FamilyName))
        {
            throw new BadRequestException("Family code and family name are required.");
        }

        var emailError = _validation.ValidateEmail(request.Email);
        if (emailError is not null)
        {
            throw new BadRequestException(emailError);
        }

        if (await _accounts.ExistsByUsernameAsync(request.Username.Trim()))
        {
            throw new ConflictException("Username already exists.");
        }

        if (await _families.ExistsByCodeAsync(request.FamilyCode.Trim()))
        {
            throw new ConflictException("Family code already exists.");
        }

        var family = await _families.CreateAsync(new InputCreateFamily
        {
            FamilyCode = request.FamilyCode.Trim(),
            FamilyName = request.FamilyName.Trim(),
            Description = request.Description
        }, request.Username.Trim());

        var account = await _accounts.CreateAsync(new InputCreateAccount
        {
            FK_Families = family.ID_Families,
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = _passwords.Hash(request.Password),
            IsActive = true
        }, request.Username.Trim());

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
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException("Username and password are required.");
        }

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
        var accountId = User.GetAccountId();

        if (await _accounts.ExistsByUsernameAsync(request.Username.Trim(), accountId))
        {
            throw new ConflictException("Username already exists.");
        }

        var emailError = _validation.ValidateEmail(request.Email);
        if (emailError is not null)
        {
            throw new BadRequestException(emailError);
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
