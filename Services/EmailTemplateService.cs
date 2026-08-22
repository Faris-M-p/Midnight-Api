using MidnightApi.Exceptions;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Services;

/// <summary>
/// Loads HTML email templates from Templates/.
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private const string OtpPlaceholder = "#{{OTP}}#";
    private const string ExpiryPlaceholder = "#{{EXPIRY_MINUTES}}#";

    private readonly string _templatesRoot;
    private readonly ILogger<EmailTemplateService> _iLogger;

    public EmailTemplateService(IWebHostEnvironment environment, ILogger<EmailTemplateService> logger)
    {
        _iLogger = logger;
        _templatesRoot = Path.Combine(environment.ContentRootPath, "Templates");
    }

    public string RegistrationSubject => "Verify your Midnight Chronicle account";

    public string ForgotPasswordSubject => "Reset your Midnight Chronicle password";

    public string BuildRegistrationVerificationHtml(string otp, int expiryMinutes) =>
        Render("RegistrationVerification.html", otp, expiryMinutes);

    public string BuildForgotPasswordHtml(string otp, int expiryMinutes) =>
        Render("ForgotPassword.html", otp, expiryMinutes);

    private string Render(string fileName, string otp, int expiryMinutes)
    {
        var path = Path.Combine(_templatesRoot, fileName);
        if (!File.Exists(path))
        {
            _iLogger.LogError("Email template not found at {TemplatePath}", path);
            throw new BadRequestException("Unable to send verification email. Please try again.");
        }

        var html = File.ReadAllText(path);
        return html
            .Replace(OtpPlaceholder, otp, StringComparison.Ordinal)
            .Replace(ExpiryPlaceholder, expiryMinutes.ToString(), StringComparison.Ordinal);
    }
}
