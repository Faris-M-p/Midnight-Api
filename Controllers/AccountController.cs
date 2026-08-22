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
    private readonly IUserAccountsRepository _iUserAccountsRepository;
    private readonly IFamiliesRepository _iFamiliesRepository;
    private readonly IPasswordService _iPasswordService;
    private readonly IJwtTokenService _iJwtTokenService;
    private readonly ICommonService _iCommonService;
    private readonly IAccountOtpService _iAccountOtpService;

    public AccountController(
        IUserAccountsRepository accounts,
        IFamiliesRepository families,
        IPasswordService passwords,
        IJwtTokenService jwt,
        ICommonService commonService,
        IAccountOtpService otpService)
    {
        _iUserAccountsRepository = accounts;
        _iFamiliesRepository = families;
        _iPasswordService = passwords;
        _iJwtTokenService = jwt;
        _iCommonService = commonService;
        _iAccountOtpService = otpService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] InputRegisterAccountView request,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);

        var email = request.Email.Trim();
        var familyName = request.FamilyName.Trim();
        var passwordHash = _iPasswordService.Hash(request.Password);
        var username = ResolveUsernameFromEmail(email);

        var existing = await _iUserAccountsRepository.GetByEmailAsync(new InputGetAccountByEmail { Email = email });
        long accountId;
        long? familyId;

        if (existing is not null)
        {
            if (existing.EmailVerified)
            {
                throw new ConflictException("An account already exists with this email. Please sign in.");
            }

            // Abandoned / unverified registration: reuse the same account (never duplicate email).
            var refresh = await _iUserAccountsRepository.UpdateAsync(new InputUpdateAccount
            {
                Id = existing.ID_UserAccounts,
                Username = existing.Username,
                Email = email,
                PasswordHash = passwordHash,
                UpdatedBy = existing.Username
            });
            _iCommonService.EnsureSuccess(refresh);

            var family = await _iFamiliesRepository.GetByIdAsync(new InputGetFamily { Id = existing.FK_Families });
            if (family is not null)
            {
                var familyUpdate = await _iFamiliesRepository.UpdateAsync(new InputUpdateFamily
                {
                    Id = family.ID_Families,
                    FamilyName = familyName,
                    Description = family.Description,
                    PhotoUrl = family.PhotoUrl,
                    UpdatedBy = existing.Username
                });
                _iCommonService.EnsureSuccess(familyUpdate);
            }

            accountId = existing.ID_UserAccounts;
            familyId = existing.FK_Families;
        }
        else
        {
            var result = await _iUserAccountsRepository.RegisterAsync(new InputRegisterAccount
            {
                FamilyName = familyName,
                Description = null,
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                CreatedBy = username
            });

            _iCommonService.EnsureSuccess(result);

            var account = await _iUserAccountsRepository.GetByIdAsync(new InputGetAccount { Id = result.ResponseCode })
                ?? throw new BadRequestException("Account was created but could not be loaded.");

            accountId = account.ID_UserAccounts;
            familyId = account.FK_Families;
        }

        var challenge = await _iAccountOtpService.IssueEmailVerificationOtpAsync(accountId, email, cancellationToken);

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
        _iCommonService.ValidateModelState(ModelState);

        // OTP → EmailVerified=true (committed) → only then issue JWT.
        var account = await _iAccountOtpService.VerifyEmailVerificationOtpAsync(request.Email, request.Otp);

        var (token, expiresAt) = _iJwtTokenService.CreateAdminToken(
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
                MaskedEmail = _iAccountOtpService.MaskEmail(account.Email)
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
        _iCommonService.ValidateModelState(ModelState);

        var account = await _iUserAccountsRepository.GetByEmailAsync(new InputGetAccountByEmail
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
                    MaskedEmail = _iAccountOtpService.MaskEmail(account.Email),
                    ResendAvailableInSeconds = 0
                },
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var challenge = await _iAccountOtpService.ResendAsync(
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
        _iCommonService.ValidateModelState(ModelState);

        var account = await _iUserAccountsRepository.GetByEmailAsync(new InputGetAccountByEmail
        {
            Email = request.Email.Trim()
        });

        if (account is null || !_iPasswordService.Verify(request.Password, account.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!account.EmailVerified)
        {
            try
            {
                await _iAccountOtpService.IssueEmailVerificationOtpAsync(
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
                    MaskedEmail = _iAccountOtpService.MaskEmail(account.Email)
                },
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var (token, expiresAt) = _iJwtTokenService.CreateAdminToken(
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
                MaskedEmail = _iAccountOtpService.MaskEmail(account.Email)
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
        _iCommonService.ValidateModelState(ModelState);

        var account = await _iUserAccountsRepository.GetByEmailAsync(new InputGetAccountByEmail
        {
            Email = request.Email.Trim()
        });

        if (account is not null && account.EmailVerified && account.IsActive)
        {
            try
            {
                await _iAccountOtpService.IssueForgotPasswordOtpAsync(
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
        _iCommonService.ValidateModelState(ModelState);

        var resetToken = await _iAccountOtpService.VerifyForgotPasswordOtpAsync(request.Email, request.Otp);

        return Ok(new ApiResponse<OutputForgotPasswordOtpVerified>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Verification successful. You can set a new password.",
            Data = new OutputForgotPasswordOtpVerified
            {
                Email = request.Email.Trim(),
                MaskedEmail = _iAccountOtpService.MaskEmail(request.Email),
                ResetToken = resetToken
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] InputResetPasswordView request)
    {
        _iCommonService.ValidateModelState(ModelState);

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new BadRequestException("Passwords do not match.");
        }

        if (!IsStrongPassword(request.NewPassword))
        {
            throw new BadRequestException(
                "Password must include upper/lowercase letters, a number, a symbol, and be at least 8 characters.");
        }

        await _iAccountOtpService.ResetPasswordAsync(request.Email, request.ResetToken, request.NewPassword);

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
        _iCommonService.ValidateModelState(ModelState);
        if (User.IsAccessTokenUser())
        {
            throw new ForbiddenException();
        }

        var account = await _iUserAccountsRepository.GetByIdAsync(new InputGetAccount { Id = User.GetAccountId() })
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
        _iCommonService.ValidateModelState(ModelState);
        if (User.IsAccessTokenUser())
        {
            throw new ForbiddenException();
        }

        var result = await _iUserAccountsRepository.UpdateAsync(new InputUpdateAccount
        {
            Id = User.GetAccountId(),
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = string.IsNullOrWhiteSpace(request.Password) ? null : _iPasswordService.Hash(request.Password),
            UpdatedBy = User.GetUsername()
        });

        return _iCommonService.ToActionResult(result, HttpContext.TraceIdentifier);
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
