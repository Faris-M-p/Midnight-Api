using MidnightApi.Models;

namespace MidnightApi.Services.Interfaces;

public interface IImageFileService
{
    Task<string> SaveAsync(
        ImageUploadRequest upload,
        HttpRequest request,
        CancellationToken cancellationToken = default);
}
