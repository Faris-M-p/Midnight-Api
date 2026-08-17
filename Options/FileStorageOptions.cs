namespace MidnightApi.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Absolute or content-root-relative path for physical files (outside wwwroot by default).
    /// </summary>
    public string RootPath { get; set; } = "App_Data/storage";

    /// <summary>
    /// Public URL path prefix used to serve local files (e.g. /media).
    /// Empty disables URL generation for local storage.
    /// </summary>
    public string RequestPath { get; set; } = "/media";

    /// <summary>
    /// Optional absolute public base URL (scheme + host). When empty, request host is used when available.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;
}
