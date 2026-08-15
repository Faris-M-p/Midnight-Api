using System.ComponentModel.DataAnnotations;
using MidnightApi.DataAccess;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Models;

// --- Frontend / API views ---

public class InputRegisterAccountView
{
    [Display(Name = "Email")]
    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Password")]
    [Required, MinLength(6), StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Family Name")]
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string FamilyName { get; set; } = string.Empty;
}

public class InputLoginView
{
    [Display(Name = "Email")]
    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;

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

// --- DB input models ---

public class InputGetAccount
{
    [DbParam("p_ID_UserAccounts")]
    public long Id { get; set; }
}

public class InputLoginAccount
{
    [DbParam("p_Username")]
    public string Username { get; set; } = string.Empty;
}

public class InputRegisterAccount
{
    [DbParam("p_FamilyCode")]
    public string FamilyCode { get; set; } = string.Empty;

    [DbParam("p_FamilyName")]
    public string FamilyName { get; set; } = string.Empty;

    [DbParam("p_Description")]
    public string? Description { get; set; }

    [DbParam("p_Username")]
    public string Username { get; set; } = string.Empty;

    [DbParam("p_Email")]
    public string Email { get; set; } = string.Empty;

    [DbParam("p_PasswordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [DbParam("p_CreatedBy")]
    public string CreatedBy { get; set; } = string.Empty;
}

public class InputUpdateAccount
{
    [DbParam("p_ID_UserAccounts")]
    public long Id { get; set; }

    [DbParam("p_Username")]
    public string Username { get; set; } = string.Empty;

    [DbParam("p_Email")]
    public string Email { get; set; } = string.Empty;

    [DbParam("p_PasswordHash")]
    public string? PasswordHash { get; set; }

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

// --- Output models ---

public class InputGetAccountByEmail
{
    [DbParam("p_Email")]
    public string Email { get; set; } = string.Empty;
}

public class InputSetEmailVerified
{
    [DbParam("p_ID_UserAccounts")]
    public long Id { get; set; }

    [DbParam("p_EmailVerified")]
    public bool EmailVerified { get; set; }

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class InputUpdateAccountPassword
{
    [DbParam("p_ID_UserAccounts")]
    public long Id { get; set; }

    [DbParam("p_PasswordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class OutputGetAccount
{
    public long ID_UserAccounts { get; set; }
    public long FK_Families { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
}

public class OutputLoginAccount
{
    public long ID_UserAccounts { get; set; }
    public long FK_Families { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
}

public class OutputRegister : CommonResponse<IdResponse>
{
}

public class OutputUpdateAccount : CommonResponse<IdResponse>
{
}

public class OutputLogin
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public bool RequiresEmailVerification { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? MaskedEmail { get; set; }
    public int ResendAvailableInSeconds { get; set; }
}

public class OutputRegisterAccount
{
    public long AccountId { get; set; }
    public long? FamilyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public bool RequiresEmailVerification { get; set; } = true;
    public int ResendAvailableInSeconds { get; set; }
}

public class InputVerifyEmailView
{
    [Display(Name = "Email")]
    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Verification code")]
    [Required, StringLength(10, MinimumLength = 4)]
    [TrimmedString]
    public string Otp { get; set; } = string.Empty;
}

public class InputResendVerificationView
{
    [Display(Name = "Email")]
    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;
}

public class InputForgotPasswordView
{
    [Display(Name = "Email")]
    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;
}

public class InputVerifyForgotPasswordOtpView
{
    [Display(Name = "Email")]
    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Verification code")]
    [Required, StringLength(10, MinimumLength = 4)]
    [TrimmedString]
    public string Otp { get; set; } = string.Empty;
}

public class InputResetPasswordView
{
    [Display(Name = "Email")]
    [Required, EmailAddress, StringLength(256)]
    [TrimmedString]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string ResetToken { get; set; } = string.Empty;

    [Display(Name = "Password")]
    [Required, MinLength(8), StringLength(100)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Confirm Password")]
    [Required, MinLength(8), StringLength(100)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class OutputForgotPasswordRequest
{
    public string Message { get; set; } =
        "If an account exists for this email, a verification code has been sent.";
}

public class OutputForgotPasswordOtpVerified
{
    public string Email { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
}
