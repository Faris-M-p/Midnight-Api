namespace MidnightApi.Models;

public sealed class FileStorageUploadRequest
{
    public required long FamilyId { get; init; }
    public required long MemoryId { get; init; }
    public required Stream Content { get; init; }
    public required string OriginalFileName { get; init; }
    public string? ContentType { get; init; }
    public long? ContentLength { get; init; }
}

public sealed class FileStorageUploadResult
{
    public required string StorageKey { get; init; }
    public string? FileUrl { get; init; }
    public required long FileSize { get; init; }
    public required string StoredFileName { get; init; }
    public required string FileType { get; init; }
    public required string MimeType { get; init; }
}
