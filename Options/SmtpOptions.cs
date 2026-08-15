namespace MidnightApi.Options;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Midnight Chronicle";
    public bool EnableSsl { get; set; } = true;
}

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int Length { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 30;
    public int PasswordResetTokenMinutes { get; set; } = 10;
}
