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
    private readonly IFamiliesRepository _families;
    private readonly IPasswordService _passwords;
    private readonly IJwtTokenService _jwt;
    private readonly ICommonService _commonService;
    private readonly IAccountOtpService _otpService;

    public AccountController(
        IUserAccountsRepository accounts,
        IFamiliesRepository families,
        IPasswordService passwords,
        IJwtTokenService jwt,
        ICommonService commonService,
        IAccountOtpService otpService)
    {
        _accounts = accounts;
        _families = families;
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

        var email = request.Email.Trim();
        var familyName = request.FamilyName.Trim();
        var passwordHash = _passwords.Hash(request.Password);
        var username = ResolveUsernameFromEmail(email);

        var existing = await _accounts.GetByEmailAsync(new InputGetAccountByEmail { Email = email });
        long accountId;
        long? familyId;

        if (existing is not null)
        {
            if (existing.EmailVerified)
            {
                throw new ConflictException("An account already exists with this email. Please sign in.");
            }

            // Abandoned / unverified registration: reuse the same account (never duplicate email).
            var refresh = await _accounts.UpdateAsync(new InputUpdateAccount
            {
                Id = existing.ID_UserAccounts,
                Username = existing.Username,
                Email = email,
                PasswordHash = passwordHash,
                UpdatedBy = existing.Username
            });
            _commonService.EnsureSuccess(refresh);

            var family = await _families.GetByIdAsync(new InputGetFamily { Id = existing.FK_Families });
            if (family is not null)
            {
                var familyUpdate = await _families.UpdateAsync(new InputUpdateFamily
                {
                    Id = family.ID_Families,
                    FamilyName = familyName,
                    Description = family.Description,
                    PhotoUrl = family.PhotoUrl,
                    UpdatedBy = existing.Username
                });
                _commonService.EnsureSuccess(familyUpdate);
            }

            accountId = existing.ID_UserAccounts;
            familyId = existing.FK_Families;
        }
        else
        {
            var result = await _accounts.RegisterAsync(new InputRegisterAccount
            {
                FamilyName = familyName,
                Description = null,
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                CreatedBy = username
            });

            _commonService.EnsureSuccess(result);

            var account = await _accounts.GetByIdAsync(new InputGetAccount { Id = result.ResponseCode })
                ?? throw new BadRequestException("Account was created but could not be loaded.");

            accountId = account.ID_UserAccounts;
            familyId = account.FK_Families;
        }

        var challenge = await _otpService.IssueEmailVerificationOtpAsync(accountId, email, cancellationToken);

        return Ok(new ApiResponse<OutputRegisterAccount>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = "We've sent a verification code to your email.",
            Data = new OutputRegisterAccount
            {
                AccountId = accountId,
                FamilyId = familyId,
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

        // OTP → EmailVerified=true (committed) → only then issue JWT.
        var account = await _otpService.VerifyEmailVerificationOtpAsync(request.Email, request.Otp);

        var (token, expiresAt) = _jwt.CreateAdminToken(
            account.ID_UserAccounts,
            account.FK_Families,
            account.Username);

        return Ok(new ApiResponse<OutputLogin>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Email verified successfully.",
            Data = new OutputLogin
            {
                AccessToken = token,
                ExpiresAtUtc = expiresAt,
                RequiresEmailVerification = false,
                Username = account.Username,
                Email = account.Email,
                MaskedEmail = _otpService.MaskEmail(account.Email)
            },
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
                Message = "We've sent a verification code to your email.",
                Data = new OutputOtpChallenge
                {
                    Email = account.Email,
                    MaskedEmail = _otpService.MaskEmail(account.Email),
                    ResendAvailableInSeconds = 0
                },
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var challenge = await _otpService.ResendAsync(
            account.ID_UserAccounts,
            account.Email,
            OtpPurposes.EmailVerification,
            cancellationToken);

        return Ok(new ApiResponse<OutputOtpChallenge>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "We've sent a verification code to your email.",
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

        var account = await _accounts.GetByEmailAsync(new InputGetAccountByEmail
        {
            Email = request.Email.Trim()
        });

        if (account is null || !_passwords.Verify(request.Password, account.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!account.EmailVerified)
        {
            try
            {
                await _otpService.IssueEmailVerificationOtpAsync(
                    account.ID_UserAccounts,
                    account.Email,
                    cancellationToken);
            }
            catch (BadRequestException)
            {
                // Still route to verification screen without exposing account state.
            }

            return Ok(new ApiResponse<OutputLogin>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "We've sent a verification code to your email.",
                Data = new OutputLogin
                {
                    RequiresEmailVerification = true,
                    Username = account.Username,
                    Email = account.Email,
                    MaskedEmail = _otpService.MaskEmail(account.Email)
                },
                TraceId = HttpContext.TraceIdentifier
            });
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
                ExpiresAtUtc = expiresAt,
                RequiresEmailVerification = false,
                Username = account.Username,
                Email = account.Email,
                MaskedEmail = _otpService.MaskEmail(account.Email)
            },
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

        if (account is not null && account.EmailVerified && account.IsActive)
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

    /// <summary>
    /// Internal account handle derived from email (UI no longer collects username).
    /// </summary>
    private static string ResolveUsernameFromEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length <= 100)
        {
            return normalized;
        }

        return normalized[..100];
    }
}
