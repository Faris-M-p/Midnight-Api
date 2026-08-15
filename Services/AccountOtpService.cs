using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Options;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Services;

public static class OtpPurposes
{
    public const string Registration = "Registration";
    public const string ForgotPassword = "ForgotPassword";
    public const string Login = "Login";
}

public class AccountOtpService : IAccountOtpService
{
    private readonly IAccountOtpsRepository _otps;
    private readonly IUserAccountsRepository _accounts;
    private readonly IEmailService _email;
    private readonly IEmailTemplateService _templates;
    private readonly IPasswordService _passwords;
    private readonly OtpOptions _options;
    private readonly ICommonService _common;

    public AccountOtpService(
        IAccountOtpsRepository otps,
        IUserAccountsRepository accounts,
        IEmailService email,
        IEmailTemplateService templates,
        IPasswordService passwords,
        IOptions<OtpOptions> options,
        ICommonService common)
    {
        _otps = otps;
        _accounts = accounts;
        _email = email;
        _templates = templates;
        _passwords = passwords;
        _options = options.Value;
        _common = common;
    }

    public string MaskEmail(string email)
    {
        var value = email.Trim();
        var at = value.IndexOf('@');
        if (at <= 0 || at == value.Length - 1)
        {
            return "***";
        }

        var local = value[..at];
        var domain = value[(at + 1)..];
        var visible = local.Length <= 3 ? local[..1] : local[..Math.Min(3, local.Length)];
        return $"{visible}********@{domain}";
    }

    public async Task<OutputOtpChallenge> IssueRegistrationOtpAsync(
        long accountId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return await IssueAndSendAsync(
            accountId,
            email,
            OtpPurposes.Registration,
            _templates.RegistrationSubject,
            _templates.BuildRegistrationVerificationHtml,
            cancellationToken);
    }

    public async Task<OutputOtpChallenge> IssueForgotPasswordOtpAsync(
        long accountId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return await IssueAndSendAsync(
            accountId,
            email,
            OtpPurposes.ForgotPassword,
            _templates.ForgotPasswordSubject,
            _templates.BuildForgotPasswordHtml,
            cancellationToken);
    }

    public async Task<OutputOtpChallenge> IssueLoginOtpAsync(
        long accountId,
        string email,
        CancellationToken cancellationToken = default)
    {
        // Fresh code on each successful password check (cooldown applies only to explicit resend).
        return await IssueAndSendAsync(
            accountId,
            email,
            OtpPurposes.Login,
            _templates.LoginSubject,
            _templates.BuildLoginVerificationHtml,
            cancellationToken,
            enforceCooldown: false);
    }

    public async Task<OutputOtpChallenge> ResendAsync(
        long accountId,
        string email,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        var active = await _otps.GetActiveAsync(new InputGetActiveAccountOtp
        {
            AccountId = accountId,
            Purpose = purpose
        });

        if (active is not null)
        {
            var waitSeconds = SecondsUntilResendAllowed(active.LastSentOn);
            if (waitSeconds > 0)
            {
                throw new BadRequestException("Please wait before requesting another code.");
            }
        }

        return purpose switch
        {
            OtpPurposes.Registration => await IssueRegistrationOtpAsync(accountId, email, cancellationToken),
            OtpPurposes.ForgotPassword => await IssueForgotPasswordOtpAsync(accountId, email, cancellationToken),
            OtpPurposes.Login => await IssueAndSendAsync(
                accountId,
                email,
                OtpPurposes.Login,
                _templates.LoginSubject,
                _templates.BuildLoginVerificationHtml,
                cancellationToken),
            _ => throw new BadRequestException("Invalid verification request.")
        };
    }

    public async Task VerifyRegistrationOtpAsync(string email, string otp)
    {
        var account = await RequireAccountByEmailAsync(email);
        if (account.EmailVerified)
        {
            return;
        }

        await VerifyOtpAsync(account, OtpPurposes.Registration, otp, createResetToken: false);

        var result = await _accounts.SetEmailVerifiedAsync(new InputSetEmailVerified
        {
            Id = account.ID_UserAccounts,
            EmailVerified = true,
            UpdatedBy = account.Username
        });
        _common.EnsureSuccess(result);
    }

    public async Task VerifyLoginOtpAsync(string email, string otp)
    {
        var account = await RequireAccountByEmailAsync(email);
        if (!account.EmailVerified)
        {
            throw new BadRequestException("Email verification is required before sign-in.");
        }

        if (!account.IsActive)
        {
            throw new UnauthorizedAccessException("This account is inactive.");
        }

        await VerifyOtpAsync(account, OtpPurposes.Login, otp, createResetToken: false);
    }

    public async Task<string> VerifyForgotPasswordOtpAsync(string email, string otp)
    {
        var account = await RequireAccountByEmailAsync(email);
        var (_, resetToken) = await VerifyOtpAsync(account, OtpPurposes.ForgotPassword, otp, createResetToken: true);
        return resetToken!;
    }

    public async Task ResetPasswordAsync(string email, string resetToken, string newPassword)
    {
        var account = await RequireAccountByEmailAsync(email);
        var otpRow = await _otps.GetLatestResetTokenAsync(new InputGetActiveAccountOtp
        {
            AccountId = account.ID_UserAccounts,
            Purpose = OtpPurposes.ForgotPassword
        }) ?? throw new BadRequestException("Invalid or expired password reset session. Please request a new code.");

        if (string.IsNullOrWhiteSpace(otpRow.ResetTokenHash)
            || otpRow.ResetTokenExpiresOn is null
            || otpRow.ResetTokenExpiresOn <= DateTime.UtcNow
            || !_passwords.Verify(resetToken.Trim(), otpRow.ResetTokenHash))
        {
            throw new BadRequestException("Invalid or expired password reset session. Please request a new code.");
        }

        var update = await _accounts.UpdatePasswordAsync(new InputUpdateAccountPassword
        {
            Id = account.ID_UserAccounts,
            PasswordHash = _passwords.Hash(newPassword),
            UpdatedBy = account.Username
        });
        _common.EnsureSuccess(update);

        await _otps.ClearResetTokenAsync(new InputAccountOtpById { Id = otpRow.ID_AccountOtps });
        await _otps.InvalidateAsync(new InputInvalidateAccountOtp
        {
            AccountId = account.ID_UserAccounts,
            Purpose = OtpPurposes.ForgotPassword,
            CancelledBy = account.Username
        });
    }

    private async Task<OutputOtpChallenge> IssueAndSendAsync(
        long accountId,
        string email,
        string purpose,
        string subject,
        Func<string, int, string> htmlFactory,
        CancellationToken cancellationToken,
        bool enforceCooldown = true)
    {
        var active = await _otps.GetActiveAsync(new InputGetActiveAccountOtp
        {
            AccountId = accountId,
            Purpose = purpose
        });

        if (enforceCooldown && active is not null)
        {
            var waitSeconds = SecondsUntilResendAllowed(active.LastSentOn);
            if (waitSeconds > 0)
            {
                throw new BadRequestException("Please wait before requesting another code.");
            }
        }

        await _otps.InvalidateAsync(new InputInvalidateAccountOtp
        {
            AccountId = accountId,
            Purpose = purpose,
            CancelledBy = "system"
        });

        var otp = GenerateNumericOtp(_options.Length);
        var expiresOn = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.ExpiryMinutes));
        var insert = await _otps.InsertAsync(new InputInsertAccountOtp
        {
            AccountId = accountId,
            Email = email.Trim(),
            OtpHash = _passwords.Hash(otp),
            Purpose = purpose,
            ExpiresOn = expiresOn,
            MaxAttempts = Math.Max(1, _options.MaxAttempts)
        });
        _common.EnsureSuccess(insert);

        await _email.SendHtmlAsync(email.Trim(), subject, htmlFactory(otp, _options.ExpiryMinutes), cancellationToken);

        return new OutputOtpChallenge
        {
            Email = email.Trim(),
            MaskedEmail = MaskEmail(email),
            ResendAvailableInSeconds = Math.Max(0, _options.ResendCooldownSeconds),
            ExpiresInSeconds = (int)Math.Max(0, (expiresOn - DateTime.UtcNow).TotalSeconds)
        };
    }

    private async Task<(OutputAccountOtp Otp, string? ResetToken)> VerifyOtpAsync(
        OutputLoginAccount account,
        string purpose,
        string otp,
        bool createResetToken)
    {
        var active = await _otps.GetActiveAsync(new InputGetActiveAccountOtp
        {
            AccountId = account.ID_UserAccounts,
            Purpose = purpose
        }) ?? throw new BadRequestException("Invalid verification code.");

        if (active.IsUsed)
        {
            throw new BadRequestException("Invalid verification code.");
        }

        if (active.ExpiresOn <= DateTime.UtcNow)
        {
            throw new BadRequestException("Verification code has expired.");
        }

        if (active.AttemptCount >= active.MaxAttempts)
        {
            throw new BadRequestException("Too many attempts. Please request a new code.");
        }

        if (!_passwords.Verify(otp.Trim(), active.OtpHash))
        {
            await _otps.IncrementAttemptAsync(new InputAccountOtpById { Id = active.ID_AccountOtps });
            var refreshed = await _otps.GetByIdAsync(new InputAccountOtpById { Id = active.ID_AccountOtps });
            if (refreshed is not null && refreshed.AttemptCount >= refreshed.MaxAttempts)
            {
                throw new BadRequestException("Too many attempts. Please request a new code.");
            }

            throw new BadRequestException("Invalid verification code.");
        }

        string? resetToken = null;
        string? resetHash = null;
        DateTime? resetExpires = null;
        if (createResetToken)
        {
            resetToken = GenerateResetToken();
            resetHash = _passwords.Hash(resetToken);
            resetExpires = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.PasswordResetTokenMinutes));
        }

        var marked = await _otps.MarkUsedAsync(new InputMarkAccountOtpUsed
        {
            Id = active.ID_AccountOtps,
            ResetTokenHash = resetHash,
            ResetTokenExpiresOn = resetExpires
        });
        _common.EnsureSuccess(marked);

        return (active, resetToken);
    }

    private async Task<OutputLoginAccount> RequireAccountByEmailAsync(string email)
    {
        return await _accounts.GetByEmailAsync(new InputGetAccountByEmail { Email = email.Trim() })
            ?? throw new BadRequestException("Invalid verification code.");
    }

    private int SecondsUntilResendAllowed(DateTime lastSentOn)
    {
        var eligibleAt = lastSentOn.ToUniversalTime().AddSeconds(Math.Max(0, _options.ResendCooldownSeconds));
        return (int)Math.Ceiling(Math.Max(0, (eligibleAt - DateTime.UtcNow).TotalSeconds));
    }

    private static string GenerateNumericOtp(int length)
    {
        var size = Math.Clamp(length, 4, 8);
        var max = (int)Math.Pow(10, size);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString($"D{size}");
    }

    private static string GenerateResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
