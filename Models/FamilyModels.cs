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
}

// --- DB input models ---

public class InputGetFamily
{
    [DbParam("p_id")]
    public long Id { get; set; }
}

public class InputCreateFamily
{
    [DbParam("p_family_code")]
    public string FamilyCode { get; set; } = string.Empty;

    [DbParam("p_family_name")]
    public string FamilyName { get; set; } = string.Empty;

    [DbParam("p_description")]
    public string? Description { get; set; }

    [DbParam("p_created_by")]
    public string CreatedBy { get; set; } = string.Empty;
}

public class InputUpdateFamily
{
    [DbParam("p_id")]
    public long Id { get; set; }

    [DbParam("p_family_name")]
    public string FamilyName { get; set; } = string.Empty;

    [DbParam("p_description")]
    public string? Description { get; set; }

    [DbParam("p_updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;
}

// --- DB / API output models ---

public class OutputGetFamily
{
    public long ID_Families { get; set; }
    public string FamilyCode { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class OutputCreateFamily : CommonResponse
{
}

public class OutputUpdateFamily : CommonResponse
{
}
