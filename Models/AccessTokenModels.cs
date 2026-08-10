using System.ComponentModel.DataAnnotations;
using MidnightApi.DataAccess;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Models;

// --- Frontend / API views ---

public class InputCreateAccessTokenView
{
    [Display(Name = "Token Name")]
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string TokenName { get; set; } = string.Empty;

    [Display(Name = "Permission")]
    [Required, StringLength(20)]
    [MidnightApi.Validation.CustomModelValidation.AllowedValues("View", "Edit")]
    public string Permission { get; set; } = string.Empty;

    [Display(Name = "Scope")]
    [Required, StringLength(40)]
    [MidnightApi.Validation.CustomModelValidation.AllowedValues("EntireFamily", "SelectedMember", "MemberDescendants")]
    public string Scope { get; set; } = string.Empty;

    [Display(Name = "Selected Member")]
    public long? MemberId { get; set; }

    [Display(Name = "Expiry")]
    [Required, StringLength(20)]
    [MidnightApi.Validation.CustomModelValidation.AllowedValues("30Days", "90Days", "6Months", "1Year", "Custom")]
    public string ExpiryPreset { get; set; } = string.Empty;

    [Display(Name = "Custom Expiry Date")]
    public DateTimeOffset? CustomExpiresOn { get; set; }
}

public class InputUpdateAccessTokenView
{
    [Display(Name = "Token Name")]
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string TokenName { get; set; } = string.Empty;

    [Display(Name = "Permission")]
    [Required, StringLength(20)]
    [MidnightApi.Validation.CustomModelValidation.AllowedValues("View", "Edit")]
    public string Permission { get; set; } = string.Empty;

    [Display(Name = "Scope")]
    [Required, StringLength(40)]
    [MidnightApi.Validation.CustomModelValidation.AllowedValues("EntireFamily", "SelectedMember", "MemberDescendants")]
    public string Scope { get; set; } = string.Empty;

    [Display(Name = "Selected Member")]
    public long? MemberId { get; set; }

    [Display(Name = "Expiry")]
    [Required, StringLength(20)]
    [MidnightApi.Validation.CustomModelValidation.AllowedValues("30Days", "90Days", "6Months", "1Year", "Custom")]
    public string ExpiryPreset { get; set; } = string.Empty;

    [Display(Name = "Custom Expiry Date")]
    public DateTimeOffset? CustomExpiresOn { get; set; }
}

public class InputAccessTokenRouteRequestView
{
    [Display(Name = "Token Id")]
    [GreaterThanZero]
    public long Id { get; set; }
}

public class InputSetAccessTokenStatusView
{
    [Display(Name = "Status")]
    [Required, StringLength(20)]
    [MidnightApi.Validation.CustomModelValidation.AllowedValues("Active", "Inactive")]
    public string Status { get; set; } = string.Empty;
}

public class InputAccessTokenLoginView
{
    [Display(Name = "Access Token")]
    [Required, StringLength(200)]
    [TrimmedString]
    public string AccessToken { get; set; } = string.Empty;
}

// --- DB input models ---

public class InputAccessTokenList
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }
}

public class InputGetAccessToken
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_AccessTokens")]
    public long TokenId { get; set; }
}

public class InputCreateAccessToken
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_TokenName")]
    public string TokenName { get; set; } = string.Empty;

    [DbParam("p_Permission")]
    public string Permission { get; set; } = string.Empty;

    [DbParam("p_Scope")]
    public string Scope { get; set; } = string.Empty;

    [DbParam("p_FK_Members")]
    public long? MemberId { get; set; }

    [DbParam("p_TokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    [DbParam("p_TokenPreview")]
    public string TokenPreview { get; set; } = string.Empty;

    [DbParam("p_ExpiresOn")]
    public DateTimeOffset ExpiresOn { get; set; }

    [DbParam("p_CreatedBy")]
    public string CreatedBy { get; set; } = string.Empty;
}

public class InputUpdateAccessToken
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_AccessTokens")]
    public long TokenId { get; set; }

    [DbParam("p_TokenName")]
    public string TokenName { get; set; } = string.Empty;

    [DbParam("p_Permission")]
    public string Permission { get; set; } = string.Empty;

    [DbParam("p_Scope")]
    public string Scope { get; set; } = string.Empty;

    [DbParam("p_FK_Members")]
    public long? MemberId { get; set; }

    [DbParam("p_ExpiresOn")]
    public DateTimeOffset ExpiresOn { get; set; }

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class InputSetAccessTokenStatus
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_AccessTokens")]
    public long TokenId { get; set; }

    [DbParam("p_NewStatus")]
    public string NewStatus { get; set; } = string.Empty;

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class InputDeleteAccessToken
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_AccessTokens")]
    public long TokenId { get; set; }

    [DbParam("p_CancelledBy")]
    public string CancelledBy { get; set; } = string.Empty;
}

// --- Output models ---

public class OutputAccessTokenItem
{
    public long Id { get; set; }
    public string TokenName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public long? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string TokenPreview { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
    public DateTimeOffset? LastUsedOn { get; set; }
    public int ActiveSessions { get; set; }
    public int UsageCount { get; set; }
}

public class OutputGetAccessToken : OutputAccessTokenItem
{
}

public class OutputCreateAccessTokenResult
{
    public long Id { get; set; }
    public string RawToken { get; set; } = string.Empty;
    public OutputGetAccessToken Token { get; set; } = null!;
}

public class OutputCreateAccessToken : CommonResponse<IdResponse>
{
}

public class OutputUpdateAccessToken : CommonResponse<IdResponse>
{
}

public class OutputSetAccessTokenStatus : CommonResponse<IdResponse>
{
}

public class OutputDeleteAccessToken : CommonResponse<IdResponse>
{
}

public class OutputAccessTokenLoginCandidate
{
    public long Id { get; set; }
    public string TokenName { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public long? MemberId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
}

public class InputAccessTokenRecordLogin
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_AccessTokens")]
    public long TokenId { get; set; }
}

public class OutputAccessTokenRecordLogin : CommonResponse<IdResponse>
{
}

public class OutputAccessTokenLogin
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public OutputAccessTokenLoginUser User { get; set; } = null!;
}

public class OutputAccessTokenLoginUser
{
    public string AuthType { get; set; } = "access_token";
    public long FamilyId { get; set; }
    public long TokenId { get; set; }
    public string TokenName { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public long? ScopeMemberId { get; set; }
    public bool IsAdmin { get; set; }
}
