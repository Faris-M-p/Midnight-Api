namespace MidnightApi.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Per-family storage quota in bytes (default 5 GB).
    /// </summary>
    public long FamilyLimitBytes { get; set; } = 5L * 1024 * 1024 * 1024;
}
