namespace MidnightApi.Services.Interfaces;

public interface IEmailService
{
    Task SendHtmlAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
