using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/accounts")]
[Tags("Accounts")]
public class AccountController : ControllerBase
{
    private readonly IUserAccountsRepository _accounts;
    private readonly IPasswordService _passwords;
    private readonly IJwtTokenService _jwt;
    private readonly ICommonService _commonService;
    private readonly IAccountOtpService _otpService;

    public AccountController(
        IUserAccountsRepository accounts,
        IPasswordService passwords,
        IJwtTokenService jwt,
        ICommonService commonService,
        IAccountOtpService otpService)
    {
        _accounts = accounts;
        _passwords = passwords;
        _jwt = jwt;
        _commonService = commonService;
        _otpService = otpService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] InputRegisterAccountView request,
        CancellationToken cancellationToken)
    {
        _commonService.ValidateModelState(ModelState);

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var result = await _accounts.RegisterAsync(new InputRegisterAccount
        {
            FamilyName = request.FamilyName.Trim(),
            Description = request.Description,
            Username = username,
            Email = email,
            PasswordHash = _passwords.Hash(request.Password),
            CreatedBy = username
        });

        _commonService.EnsureSuccess(result);

        var account = await _accounts.GetByIdAsync(new InputGetAccount { Id = result.ResponseCode })
            ?? throw new BadRequestException("Account was created but could not be loaded.");

        var challenge = await _otpService.IssueRegistrationOtpAsync(
            account.ID_UserAccounts,
            account.Email,
            cancellationToken);

        return Ok(new ApiResponse<OutputRegisterAccount>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = "Account created. Please verify your email.",
            Data = new OutputRegisterAccount
            {
                AccountId = account.ID_UserAccounts,
                FamilyId = account.FK_Families,
                Email = challenge.Email,
                MaskedEmail = challenge.MaskedEmail,
                RequiresEmailVerification = true,
                ResendAvailableInSeconds = challenge.ResendAvailableInSeconds
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] InputVerifyEmailView request)
    {
        _commonService.ValidateModelState(ModelState);
        await _otpService.VerifyRegistrationOtpAsync(request.Email, request.Otp);

        return Ok(new ApiResponse<object?>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Email verified successfully. You can sign in now.",
            Data = null,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification(
        [FromBody] InputResendVerificationView request,
        CancellationToken cancellationToken)
    {
        _commonService.ValidateModelState(ModelState);

        var account = await _accounts.GetByEmailAsync(new InputGetAccountByEmail
        {
            Email = request.Email.Trim()
        });

        if (account is null)
        {
            throw new BadRequestException("Unable to send verification email. Please try again.");
        }

        if (account.EmailVerified)
        {
            return Ok(new ApiResponse<OutputOtpChallenge>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Email is already verified.",
                Data = new OutputOtpChallenge
                {
                    Email = account.Email,
                    MaskedEmail = _otpService.MaskEmail(account.Email),
                    ResendAvailableInSeconds = 0,
                    ExpiresInSeconds = 0
                },
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var challenge = await _otpService.ResendAsync(
            account.ID_UserAccounts,
            account.Email,
            OtpPurposes.Registration,
            cancellationToken);

        return Ok(new ApiResponse<OutputOtpChallenge>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Verification code sent.",
            Data = challenge,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] InputLoginView request,
        CancellationToken cancellationToken)
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

        if (!account.EmailVerified)
        {
            try
            {
                await _otpService.ResendAsync(
                    account.ID_UserAccounts,
                    account.Email,
                    OtpPurposes.Registration,
                    cancellationToken);
            }
            catch (BadRequestException)
            {
                // Cooldown or send failure — still route user to verification screen.
            }

            return Ok(new ApiResponse<OutputLogin>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Email verification required.",
                Data = new OutputLogin
                {
                    RequiresEmailVerification = true,
                    RequiresLoginOtp = false,
                    Username = account.Username,
                    Email = account.Email,
                    MaskedEmail = _otpService.MaskEmail(account.Email)
                },
                TraceId = HttpContext.TraceIdentifier
            });
        }

        OutputOtpChallenge? challenge = null;
        try
        {
            challenge = await _otpService.IssueLoginOtpAsync(
                account.ID_UserAccounts,
                account.Email,
                cancellationToken);
        }
        catch (BadRequestException)
        {
            // Still route to OTP screen if send/cooldown fails after credentials succeed.
        }

        return Ok(new ApiResponse<OutputLogin>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Enter the verification code sent to your email.",
            Data = new OutputLogin
            {
                RequiresEmailVerification = false,
                RequiresLoginOtp = true,
                Username = account.Username,
                Email = account.Email,
                MaskedEmail = challenge?.MaskedEmail ?? _otpService.MaskEmail(account.Email),
                ResendAvailableInSeconds = challenge?.ResendAvailableInSeconds ?? 0
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("login/verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] InputVerifyLoginOtpView request)
    {
        _commonService.ValidateModelState(ModelState);
        await _otpService.VerifyLoginOtpAsync(request.Email, request.Otp);

        var account = await _accounts.GetByEmailAsync(new InputGetAccountByEmail
        {
            Email = request.Email.Trim()
        }) ?? throw new BadRequestException("Invalid verification code.");

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
                ExpiresAtUtc = expiresAt,
                RequiresEmailVerification = false,
                RequiresLoginOtp = false,
                Username = account.Username,
                Email = account.Email,
                MaskedEmail = _otpService.MaskEmail(account.Email)
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("login/resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendLoginOtp(
        [FromBody] InputResendLoginOtpView request,
        CancellationToken cancellationToken)
    {
        _commonService.ValidateModelState(ModelState);

        var account = await _accounts.GetByEmailAsync(new InputGetAccountByEmail
        {
            Email = request.Email.Trim()
        });

        if (account is null || !account.EmailVerified || !account.IsActive)
        {
            throw new BadRequestException("Unable to send verification email. Please try again.");
        }

        var challenge = await _otpService.ResendAsync(
            account.ID_UserAccounts,
            account.Email,
            OtpPurposes.Login,
            cancellationToken);

        return Ok(new ApiResponse<OutputOtpChallenge>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Verification code sent.",
            Data = challenge,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] InputForgotPasswordView request,
        CancellationToken cancellationToken)
    {
        _commonService.ValidateModelState(ModelState);

        var account = await _accounts.GetByEmailAsync(new InputGetAccountByEmail
        {
            Email = request.Email.Trim()
        });

        if (account is not null)
        {
            try
            {
                await _otpService.IssueForgotPasswordOtpAsync(
                    account.ID_UserAccounts,
                    account.Email,
                    cancellationToken);
            }
            catch (BadRequestException)
            {
                // Keep response generic — do not reveal send/cooldown details for enumeration.
            }
        }

        return Ok(new ApiResponse<OutputForgotPasswordRequest>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "If an account exists for this email, a verification code has been sent.",
            Data = new OutputForgotPasswordRequest(),
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("forgot-password/verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] InputVerifyForgotPasswordOtpView request)
    {
        _commonService.ValidateModelState(ModelState);

        var resetToken = await _otpService.VerifyForgotPasswordOtpAsync(request.Email, request.Otp);

        return Ok(new ApiResponse<OutputForgotPasswordOtpVerified>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Verification successful. You can set a new password.",
            Data = new OutputForgotPasswordOtpVerified
            {
                Email = request.Email.Trim(),
                MaskedEmail = _otpService.MaskEmail(request.Email),
                ResetToken = resetToken
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] InputResetPasswordView request)
    {
        _commonService.ValidateModelState(ModelState);

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new BadRequestException("Passwords do not match.");
        }

        if (!IsStrongPassword(request.NewPassword))
        {
            throw new BadRequestException(
                "Password must include upper/lowercase letters, a number, a symbol, and be at least 8 characters.");
        }

        await _otpService.ResetPasswordAsync(request.Email, request.ResetToken, request.NewPassword);

        return Ok(new ApiResponse<object?>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Password updated successfully. You can sign in now.",
            Data = null,
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

    private static bool IsStrongPassword(string password) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            password,
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$");
}
