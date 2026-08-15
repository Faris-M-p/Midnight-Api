using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Services;

public class ImageFileService : IImageFileService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    private const long MaxFileBytes = 5 * 1024 * 1024;

    public async Task<string> SaveAsync(
        ImageUploadRequest upload,
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        var file = upload.File;
        if (file.Length == 0)
        {
            throw new BadRequestException("Please choose an image.");
        }

        if (file.Length > MaxFileBytes)
        {
            throw new BadRequestException("Image must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new BadRequestException("Please choose a JPG, PNG, WEBP, or GIF image.");
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType)
            && !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Please choose an image file.");
        }

        if (upload.FamilyId <= 0)
        {
            throw new BadRequestException("Family is required to save an image.");
        }

        var segments = ResolveFolder(upload);
        var physicalFolder = Path.Combine([Directory.GetCurrentDirectory(), "wwwroot", "uploads", ..segments]);
        Directory.CreateDirectory(physicalFolder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(physicalFolder, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
        return $"{baseUrl}/uploads/{string.Join('/', segments)}/{fileName}";
    }

    private static string[] ResolveFolder(ImageUploadRequest upload)
    {
        var familyId = upload.FamilyId.ToString();
        var entityId = upload.EntityId?.ToString();

        return upload.Mode switch
        {
            ImageUploadMode.MemberProfile => entityId is null
                ? ["members", familyId]
                : ["members", familyId, entityId],
            ImageUploadMode.FamilyLogo => ["family", familyId, "logo"],
            ImageUploadMode.FamilyCover => ["family", familyId, "cover"],
            ImageUploadMode.Gallery => entityId is null
                ? ["gallery", familyId]
                : ["gallery", familyId, entityId],
            ImageUploadMode.Story => entityId is null
                ? ["stories", familyId]
                : ["stories", familyId, entityId],
            ImageUploadMode.Event => entityId is null
                ? ["events", familyId]
                : ["events", familyId, entityId],
            _ => throw new BadRequestException("Unsupported image upload mode.")
        };
    }
}
