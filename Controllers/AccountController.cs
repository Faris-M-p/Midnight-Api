using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Models;
using MidnightApi.Services;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/accounts")]
[Tags("Accounts")]
public class AccountController : ControllerBase
{
    private readonly IUserAccountsRepository _accounts;
    private readonly PasswordService _passwords;
    private readonly JwtTokenService _jwt;
    private readonly CommonService _commonService;

    public AccountController(
        IUserAccountsRepository accounts,
        PasswordService passwords,
        JwtTokenService jwt,
        CommonService commonService)
    {
        _accounts = accounts;
        _passwords = passwords;
        _jwt = jwt;
        _commonService = commonService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] InputRegisterAccountView request)
    {
        _commonService.ValidateModelState(ModelState);

        var username = request.Username.Trim();
        var result = await _accounts.RegisterAsync(new InputRegisterAccount
        {
            FamilyName = request.FamilyName.Trim(),
            Description = request.Description,
            Username = username,
            Email = request.Email.Trim(),
            PasswordHash = _passwords.Hash(request.Password),
            CreatedBy = username
        });

        return _commonService.ToActionResult(result, HttpContext.TraceIdentifier, StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] InputLoginView request)
    {
        _commonService.ValidateModelState(ModelState);

        var account = await _accounts.GetLoginByUsernameAsync(new InputLoginAccount
        {
            Username = request.Username.Trim()
        });

        if (account is null || !_passwords.Verify(request.Password, account.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
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

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        _commonService.ValidateModelState(ModelState);
        if (User.IsAccessTokenUser())
        {
            throw new ForbiddenException();
        }

        var account = await _accounts.GetByIdAsync(new InputGetAccount { Id = User.GetAccountId() })
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

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] InputUpdateAccountView request)
    {
        _commonService.ValidateModelState(ModelState);
        if (User.IsAccessTokenUser())
        {
            throw new ForbiddenException();
        }

        var result = await _accounts.UpdateAsync(new InputUpdateAccount
        {
            Id = User.GetAccountId(),
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = string.IsNullOrWhiteSpace(request.Password) ? null : _passwords.Hash(request.Password),
            UpdatedBy = User.GetUsername()
        });

        return _commonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }
}
