using MidnightApi.Models;

namespace MidnightApi.Services.Interfaces;

public interface IImageFileService
{
    Task<ImageSaveResult> SaveAsync(
        ImageUploadRequest upload,
        HttpRequest request,
        CancellationToken cancellationToken = default);
}
