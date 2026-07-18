namespace MidnightApi.Models.Api;

public class InputUpdateFamily
{
    public string FamilyName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class InputUpdateFamilyView
{
    public string FamilyName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class InputCreateFamily
{
    public string FamilyCode { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class OutputGetFamily
{
    public long ID_Families { get; set; }
    public string FamilyCode { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
