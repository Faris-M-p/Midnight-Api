using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using MidnightApi.Exceptions;
using MidnightApi.Options;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Services;

public class EmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendHtmlAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrWhiteSpace(_options.Password)
            || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogError("SMTP is not configured.");
            throw new BadRequestException("Unable to send verification email. Please try again.");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail.Trim()));

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_options.Username, _options.Password)
            };

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (BadRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {EmailDomain}", MaskEmailDomain(toEmail));
            throw new BadRequestException("Unable to send verification email. Please try again.");
        }
    }

    private static string MaskEmailDomain(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 ? $"***{email[at..]}" : "***";
    }
}
