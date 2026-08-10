using System.ComponentModel.DataAnnotations;
using MidnightApi.DataAccess;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Models;

// --- Frontend / API views ---

public class InputUpdateFamilyView
{
    [Display(Name = "Family Name")]
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string FamilyName { get; set; } = string.Empty;

    [StringLength(1000)]
    [NoScriptTags]
    public string? Description { get; set; }

    [StringLength(2000)]
    public string? PhotoUrl { get; set; }

    public IFormFile? FamilyPhoto { get; set; }
}

// --- DB input models ---

public class InputGetFamily
{
    [DbParam("p_ID_Families")]
    public long Id { get; set; }
}

public class InputGetFamilyByCode
{
    [DbParam("p_FamilyCode")]
    public string FamilyCode { get; set; } = string.Empty;
}

public class OutputFamilyByCode
{
    public long Id { get; set; }
    public string FamilyCode { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
}

public class InputCreateFamily
{
    [DbParam("p_FamilyCode")]
    public string FamilyCode { get; set; } = string.Empty;

    [DbParam("p_FamilyName")]
    public string FamilyName { get; set; } = string.Empty;

    [DbParam("p_Description")]
    public string? Description { get; set; }

    [DbParam("p_CreatedBy")]
    public string CreatedBy { get; set; } = string.Empty;
}

public class InputUpdateFamily
{
    [DbParam("p_ID_Families")]
    public long Id { get; set; }

    [DbParam("p_FamilyName")]
    public string FamilyName { get; set; } = string.Empty;

    [DbParam("p_Description")]
    public string? Description { get; set; }

    [DbParam("p_PhotoUrl")]
    public string? PhotoUrl { get; set; }

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

// --- DB / API output models ---

public class OutputGetFamily
{
    public long ID_Families { get; set; }
    public long Id => ID_Families;
    public string FamilyCode { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
}

public class OutputCreateFamily : CommonResponse<IdResponse>
{
}

public class OutputUpdateFamily : CommonResponse<IdResponse>
{
}
