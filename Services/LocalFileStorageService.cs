using Microsoft.Extensions.Options;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Options;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Services;

/// <summary>
/// Local/server filesystem implementation of <see cref="IFileStorageService"/>.
/// Storage keys are relative paths under the configured root (portable to object storage).
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly Dictionary<string, string> ExtensionMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private readonly FileStorageOptions _options;
    private readonly IHttpContextAccessor _iHttpContextAccessor;
    private readonly string _rootPath;

    public LocalFileStorageService(
        IOptions<FileStorageOptions> options,
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor)
    {
        _options = options.Value;
        _iHttpContextAccessor = httpContextAccessor;
        _rootPath = ResolveRootPath(_options.RootPath, environment.ContentRootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public string RootPath => _rootPath;

    public static string ResolveRootPath(string? configuredRoot, string contentRoot)
    {
        var root = string.IsNullOrWhiteSpace(configuredRoot) ? "App_Data/storage" : configuredRoot.Trim();
        if (Path.IsPathRooted(root))
        {
            return Path.GetFullPath(root);
        }

        return Path.GetFullPath(Path.Combine(contentRoot, root));
    }

    public async Task<FileStorageUploadResult> UploadAsync(
        FileStorageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        if (request.FamilyId <= 0)
        {
            throw new BadRequestException("Family is required to store a file.");
        }

        if (request.MemoryId <= 0 && request.EventId < 0)
        {
            throw new BadRequestException("A memory or event is required to store a file.");
        }

        var extension = NormalizeExtension(Path.GetExtension(request.OriginalFileName));
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new BadRequestException("Please choose a JPG, JPEG, PNG, or WEBP image.");
        }

        ValidateContentType(request.ContentType, extension);

        var maxBytes = _options.MaxFileBytes > 0 ? _options.MaxFileBytes : 10 * 1024 * 1024;
        if (request.ContentLength is > 0 and var declared && declared > maxBytes)
        {
            throw new BadRequestException("Image must be 10 MB or smaller.");
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storageKey = request.EventId > 0
            ? BuildEventStorageKey(request.FamilyId, request.EventId, storedFileName)
            : request.MemoryId > 0
                ? BuildMemoryStorageKey(request.FamilyId, request.MemoryId, storedFileName)
                : BuildEventStagingStorageKey(request.FamilyId, storedFileName);
        var physicalPath = ResolvePhysicalPath(storageKey);
        var physicalFolder = Path.GetDirectoryName(physicalPath)
            ?? throw new InvalidOperationException("Unable to resolve storage folder.");

        Directory.CreateDirectory(physicalFolder);

        long written;
        try
        {
            await using var output = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            written = await CopyWithLimitAsync(request.Content, output, maxBytes, cancellationToken);
        }
        catch
        {
            TryDeletePhysical(physicalPath);
            throw;
        }

        if (written <= 0)
        {
            TryDeletePhysical(physicalPath);
            throw new BadRequestException("Please choose an image.");
        }

        var mimeType = ResolveMimeType(request.ContentType, extension);
        return new FileStorageUploadResult
        {
            StorageKey = storageKey,
            FileUrl = GetUrl(storageKey),
            FileSize = written,
            StoredFileName = storedFileName,
            FileType = extension.TrimStart('.').ToLowerInvariant(),
            MimeType = mimeType
        };
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePhysicalPath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePhysicalPath(storageKey);
        return Task.FromResult(File.Exists(path));
    }

    public string? GetUrl(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var requestPath = NormalizeRequestPath(_options.RequestPath);
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return null;
        }

        var relative = $"{requestPath}/{storageKey.Replace('\\', '/').Trim('/')}";
        var publicBase = _options.PublicBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(publicBase))
        {
            return $"{publicBase}{relative}";
        }

        var http = _iHttpContextAccessor.HttpContext?.Request;
        if (http is null)
        {
            return relative;
        }

        var baseUrl = $"{http.Scheme}://{http.Host}{http.PathBase}".TrimEnd('/');
        return $"{baseUrl}{relative}";
    }

    public static string BuildMemoryStorageKey(long familyId, long memoryId, string storedFileName) =>
        $"families/{familyId}/memories/{memoryId}/{storedFileName}";

    public static string BuildEventStorageKey(long familyId, long eventId, string storedFileName) =>
        $"families/{familyId}/events/{eventId}/{storedFileName}";

    public static string BuildEventStagingStorageKey(long familyId, string storedFileName) =>
        $"families/{familyId}/events/staging/{storedFileName}";

    private string ResolvePhysicalPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new BadRequestException("Storage key is required.");
        }

        var normalizedKey = storageKey.Replace('\\', '/').Trim('/');
        if (normalizedKey.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(normalizedKey))
        {
            throw new BadRequestException("Invalid storage key.");
        }

        var combined = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootFull = Path.GetFullPath(_rootPath).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(combined, Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid storage key.");
        }

        return combined;
    }

    private static string? NormalizeRequestPath(string? requestPath)
    {
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return null;
        }

        var value = requestPath.Trim().Replace('\\', '/');
        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        return value.TrimEnd('/');
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var value = extension.Trim().ToLowerInvariant();
        return value.StartsWith('.') ? value : "." + value;
    }

    private static void ValidateContentType(string? contentType, string extension)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return;
        }

        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Please choose an image file.");
        }

        var expected = ExtensionMimeTypes[extension];
        if (!contentType.Equals(expected, StringComparison.OrdinalIgnoreCase)
            && !(extension is ".jpg" or ".jpeg"
                && contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase)))
        {
            // Allow generic image/* when browser is vague, but reject mismatched known types.
            if (contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("jpg", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("png", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("webp", StringComparison.OrdinalIgnoreCase))
            {
                if (!contentType.Contains(extension.TrimStart('.'), StringComparison.OrdinalIgnoreCase)
                    && !(extension is ".jpg" or ".jpeg"
                        && contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new BadRequestException("Image type does not match the file extension.");
                }
            }
        }
    }

    private static string ResolveMimeType(string? contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType)
            && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        }

        return ExtensionMimeTypes[extension];
    }

    private static async Task<long> CopyWithLimitAsync(
        Stream source,
        Stream destination,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read <= 0)
            {
                break;
            }

            total += read;
            if (total > maxBytes)
            {
                throw new BadRequestException("Image must be 10 MB or smaller.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return total;
    }

    private static void TryDeletePhysical(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup after failed upload.
        }
    }
}
