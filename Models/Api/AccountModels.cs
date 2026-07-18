using System.ComponentModel.DataAnnotations;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Models.Api;

public class InputRegisterAccountView
{
    [Display(Name = "Username")]
    [Required, MinLength(3), StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Password")]
    [Required, MinLength(6), StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Family Code")]
    [Required, StringLength(50)]
    [TrimmedString]
    [NoScriptTags]
    public string FamilyCode { get; set; } = string.Empty;

    [Display(Name = "Family Name")]
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string FamilyName { get; set; } = string.Empty;

    [StringLength(1000)]
    [NoScriptTags]
    public string? Description { get; set; }
}

public class InputLoginView
{
    [Display(Name = "Username")]
    [Required, MinLength(3), StringLength(100)]
    [TrimmedString]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Password")]
    [Required, MinLength(6), StringLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class InputUpdateAccountView
{
    [Display(Name = "Username")]
    [Required, MinLength(3), StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;

    [MinLength(6), StringLength(100)]
    public string? Password { get; set; }
}

public class InputCreateAccount
{
    [GreaterThanZero]
    public long FK_Families { get; set; }

    [Required, MinLength(3), StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(10), StringLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class InputUpdateAccount
{
    [Required, MinLength(3), StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;

    [MinLength(10), StringLength(500)]
    public string? PasswordHash { get; set; }
}

public class OutputGetAccount
{
    public long ID_UserAccounts { get; set; }
    public long FK_Families { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class OutputLogin
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class OutputRegister
{
    public long AccountId { get; set; }
    public long FamilyId { get; set; }
    public string Username { get; set; } = string.Empty;
}
