using MidnightApi.Models;

namespace MidnightApi.Services.Interfaces;

public interface IAccountOtpService
{
    string MaskEmail(string email);

    Task<OutputOtpChallenge> IssueEmailVerificationOtpAsync(
        long accountId,
        string email,
        CancellationToken cancellationToken = default);

    Task<OutputOtpChallenge> IssueForgotPasswordOtpAsync(
        long accountId,
        string email,
        CancellationToken cancellationToken = default);

    Task<OutputOtpChallenge> ResendAsync(
        long accountId,
        string email,
        string purpose,
        CancellationToken cancellationToken = default);

    Task<OutputLoginAccount> VerifyEmailVerificationOtpAsync(string email, string otp);

    Task<string> VerifyForgotPasswordOtpAsync(string email, string otp);

    Task ResetPasswordAsync(string email, string resetToken, string newPassword);
}
