using MidnightApi.Models;

namespace MidnightApi.Services.Interfaces;

public interface IAccountOtpService
{
    string MaskEmail(string email);

    Task<OutputOtpChallenge> IssueRegistrationOtpAsync(
        long accountId,
        string email,
        CancellationToken cancellationToken = default);

    Task<OutputOtpChallenge> IssueForgotPasswordOtpAsync(
        long accountId,
        string email,
        CancellationToken cancellationToken = default);

    Task<OutputOtpChallenge> IssueLoginOtpAsync(
        long accountId,
        string email,
        CancellationToken cancellationToken = default);

    Task<OutputOtpChallenge> ResendAsync(
        long accountId,
        string email,
        string purpose,
        CancellationToken cancellationToken = default);

    Task VerifyRegistrationOtpAsync(string email, string otp);

    Task VerifyLoginOtpAsync(string email, string otp);

    Task<string> VerifyForgotPasswordOtpAsync(string email, string otp);

    Task ResetPasswordAsync(string email, string resetToken, string newPassword);
}
