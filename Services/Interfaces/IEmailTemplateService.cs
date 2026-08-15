namespace MidnightApi.Services.Interfaces;

public interface IEmailTemplateService
{
    string RegistrationSubject { get; }
    string ForgotPasswordSubject { get; }
    string BuildRegistrationVerificationHtml(string otp, int expiryMinutes);
    string BuildForgotPasswordHtml(string otp, int expiryMinutes);
}
