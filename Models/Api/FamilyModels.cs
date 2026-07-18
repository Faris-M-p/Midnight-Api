using System.ComponentModel.DataAnnotations;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Models.Api;

public class InputUpdateFamily
{
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string FamilyName { get; set; } = string.Empty;

    [StringLength(1000)]
    [NoScriptTags]
    public string? Description { get; set; }
}

public class InputUpdateFamilyView
{
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string FamilyName { get; set; } = string.Empty;

    [StringLength(1000)]
    [NoScriptTags]
    public string? Description { get; set; }
}

public class InputCreateFamily
{
    [Required, StringLength(50)]
    [TrimmedString]
    [NoScriptTags]
    public string FamilyCode { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string FamilyName { get; set; } = string.Empty;

    [StringLength(1000)]
    [NoScriptTags]
    public string? Description { get; set; }
}

public class OutputGetFamily
{
    public long ID_Families { get; set; }
    public string FamilyCode { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
