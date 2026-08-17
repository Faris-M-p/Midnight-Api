using MidnightApi.Models;

namespace MidnightApi.Services.Interfaces;

/// <summary>
/// Provider-agnostic file storage (local disk today; R2/S3 later).
/// </summary>
public interface IFileStorageService
{
    Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    string? GetUrl(string storageKey);
}
