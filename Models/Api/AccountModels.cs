namespace MidnightApi.Models.Api;

using MidnightApi.Models.Api;

public class InputCreateAccount
{
    public long FK_Members { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class InputUpdateAccount
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; }
}

public class InputCreateAccountView
{
    public long FK_Members { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class InputUpdateAccountView
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool IsActive { get; set; }
}

public class OutputGetAccount
{
    public long ID_UserAccounts { get; set; }
    public long FK_Members { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
