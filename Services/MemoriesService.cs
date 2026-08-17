using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Services;

public class MemoriesService : IMemoriesService
{
    private const int MaxImagesPerMemory = 10;

    private static readonly HashSet<string> AllowedSort = new(StringComparer.OrdinalIgnoreCase)
    {
        "recent",
        "oldest",
        "title"
    };

    private readonly IMemoriesRepository _memories;
    private readonly IFileStorageService _files;
    private readonly ICommonService _common;
    private readonly IFamilyStorageService _storage;
    private readonly ILogger<MemoriesService> _logger;

    public MemoriesService(
        IMemoriesRepository memories,
        IFileStorageService files,
        ICommonService common,
        IFamilyStorageService storage,
        ILogger<MemoriesService> logger)
    {
        _memories = memories;
        _files = files;
        _common = common;
        _storage = storage;
        _logger = logger;
    }

    public async Task<OutputPagedMemories> ListAsync(long familyId, InputMemoryListQueryView query)
    {
        var sortBy = NormalizeSort(query.SortBy);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 12 : query.PageSize, 1, 50);

        return await _memories.GetListAsync(new InputMemoryList
        {
            FamilyId = familyId,
            Search = query.Search,
            SortBy = sortBy,
            Page = page,
            PageSize = pageSize
        }) ?? new OutputPagedMemories
        {
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OutputGetMemory> GetAsync(long familyId, long memoryId)
    {
        return await _memories.GetByIdAsync(new InputGetMemory
        {
            FamilyId = familyId,
            Id = memoryId
        }) ?? throw new NotFoundException("Memory not found.");
    }

    public async Task<OutputGetMemory> CreateAsync(
        long familyId,
        string username,
        InputCreateMemoryView request,
        CancellationToken cancellationToken = default)
    {
        if (request.CoverImage is null || request.CoverImage.Length <= 0)
        {
            throw new BadRequestException("Cover image is required.");
        }

        var extraImages = (request.Images ?? [])
            .Where(file => file is { Length: > 0 })
            .ToList();
        if (1 + extraImages.Count > MaxImagesPerMemory)
        {
            throw new BadRequestException("A memory can have at most 10 images.");
        }

        var create = await _memories.CreateAsync(new InputCreateMemory
        {
            FamilyId = familyId,
            Title = request.Title,
            Description = request.Description,
            MemoryDate = request.MemoryDate,
            Location = request.Location,
            CreatedBy = username
        });
        _common.EnsureSuccess(create);

        var memoryId = create.ResponseCode;
        try
        {
            await UploadAndCommitImageAsync(
                familyId,
                memoryId,
                request.CoverImage,
                username,
                setAsCover: true,
                cancellationToken);

            foreach (var image in extraImages)
            {
                await UploadAndCommitImageAsync(
                    familyId,
                    memoryId,
                    image,
                    username,
                    setAsCover: false,
                    cancellationToken);
            }
        }
        catch
        {
            await SafeDeleteMemoryAsync(familyId, memoryId, username, cancellationToken);
            throw;
        }

        return await GetAsync(familyId, memoryId);
    }

    public async Task<OutputGetMemory> UpdateAsync(
        long familyId,
        long memoryId,
        string username,
        InputUpdateMemoryView request,
        CancellationToken cancellationToken = default)
    {
        _ = await GetAsync(familyId, memoryId);

        var update = await _memories.UpdateAsync(new InputUpdateMemory
        {
            FamilyId = familyId,
            Id = memoryId,
            Title = request.Title,
            Description = request.Description,
            MemoryDate = request.MemoryDate,
            Location = request.Location,
            UpdatedBy = username
        });
        _common.EnsureSuccess(update);

        if (request.CoverImage is not null && request.CoverImage.Length > 0)
        {
            await UploadAndCommitImageAsync(
                familyId,
                memoryId,
                request.CoverImage,
                username,
                setAsCover: true,
                cancellationToken);
        }

        return await GetAsync(familyId, memoryId);
    }

    public async Task DeleteAsync(
        long familyId,
        long memoryId,
        string username,
        CancellationToken cancellationToken = default)
    {
        var memory = await GetAsync(familyId, memoryId);
        await DeletePhysicalImagesAsync(familyId, memoryId, memory.Images, cancellationToken);

        var result = await _memories.SoftDeleteAsync(new InputDeleteMemory
        {
            FamilyId = familyId,
            Id = memoryId,
            CancelledBy = username
        });
        _common.EnsureSuccess(result);
    }

    public async Task<OutputMemoryImageAction> UploadImageAsync(
        long familyId,
        long memoryId,
        string username,
        InputUploadMemoryImageView request,
        CancellationToken cancellationToken = default)
    {
        if (request.Image is null || request.Image.Length <= 0)
        {
            throw new BadRequestException("Image is required.");
        }

        return await UploadAndCommitImageAsync(
            familyId,
            memoryId,
            request.Image,
            username,
            setAsCover: false,
            cancellationToken);
    }

    public async Task<OutputMemoryImageAction> DeleteImageAsync(
        long familyId,
        long memoryId,
        long imageId,
        string username,
        CancellationToken cancellationToken = default)
    {
        var image = await _memories.GetImageAsync(new InputMemoryImageGet
        {
            FamilyId = familyId,
            MemoryId = memoryId,
            ImageId = imageId
        }) ?? throw new NotFoundException("Image not found.");

        if (string.IsNullOrWhiteSpace(image.StorageKey))
        {
            throw new BadRequestException("Image storage key is missing.");
        }

        try
        {
            if (await _files.ExistsAsync(image.StorageKey, cancellationToken))
            {
                await _files.DeleteAsync(image.StorageKey, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete physical memory image. FamilyId={FamilyId}, MemoryId={MemoryId}, ImageId={ImageId}, StorageKey={StorageKey}",
                familyId,
                memoryId,
                imageId,
                image.StorageKey);
            throw new BadRequestException(
                "Unable to delete the image file. Please try again.",
                ex.Message);
        }

        var deleted = await _memories.SoftDeleteImageAsync(new InputMemoryImageDelete
        {
            FamilyId = familyId,
            MemoryId = memoryId,
            ImageId = imageId,
            CancelledBy = username
        });
        _common.EnsureSuccess(deleted);

        var usage = await _storage.GetStorageUsageAsync(familyId);
        var payload = deleted.Data ?? new OutputMemoryImageAction
        {
            ImageId = imageId,
            FileSize = image.FileSize,
            MaxImageCount = MaxImagesPerMemory
        };
        payload.StorageUsedBytes = usage.StorageUsedBytes;
        payload.StorageLimitBytes = usage.StorageLimitBytes;
        return payload;
    }

    public async Task<OutputGetMemory> SetCoverAsync(
        long familyId,
        long memoryId,
        long imageId,
        string username)
    {
        if (imageId <= 0)
        {
            throw new BadRequestException("Image is required.");
        }

        var result = await _memories.SetCoverAsync(new InputMemoryCoverSet
        {
            FamilyId = familyId,
            MemoryId = memoryId,
            ImageId = imageId,
            UpdatedBy = username
        });
        _common.EnsureSuccess(result);
        return await GetAsync(familyId, memoryId);
    }

    private async Task<OutputMemoryImageAction> UploadAndCommitImageAsync(
        long familyId,
        long memoryId,
        IFormFile image,
        string username,
        bool setAsCover,
        CancellationToken cancellationToken)
    {
        var context = await _memories.GetImageUploadContextAsync(new InputMemoryImageUploadContext
        {
            FamilyId = familyId,
            MemoryId = memoryId
        }) ?? throw new NotFoundException("Memory not found.");

        if (context.ImageCount >= MaxImagesPerMemory)
        {
            throw new BadRequestException("A memory can have at most 10 images.");
        }

        if (image.Length > 0)
        {
            await _storage.EnsureCanUploadAsync(familyId, image.Length);
        }

        FileStorageUploadResult uploaded;
        await using (var stream = image.OpenReadStream())
        {
            uploaded = await _files.UploadAsync(new FileStorageUploadRequest
            {
                FamilyId = familyId,
                MemoryId = memoryId,
                Content = stream,
                OriginalFileName = image.FileName,
                ContentType = image.ContentType,
                ContentLength = image.Length
            }, cancellationToken);
        }

        try
        {
            await _storage.EnsureCanUploadAsync(familyId, uploaded.FileSize);
        }
        catch
        {
            await SafeDeletePhysicalAsync(uploaded.StorageKey, cancellationToken);
            throw;
        }

        try
        {
            var commit = await _memories.CommitImageAsync(new InputMemoryImageCommit
            {
                FamilyId = familyId,
                MemoryId = memoryId,
                FileName = uploaded.StoredFileName,
                StorageKey = uploaded.StorageKey,
                ImageUrl = uploaded.FileUrl,
                MimeType = uploaded.MimeType,
                FileSize = uploaded.FileSize,
                SortOrder = context.NextSortOrder,
                SetAsCover = setAsCover,
                CreatedBy = username
            });
            _common.EnsureSuccess(commit);

            var usage = await _storage.GetStorageUsageAsync(familyId);
            return commit.Data is null
                ? new OutputMemoryImageAction
                {
                    ImageId = commit.ResponseCode,
                    Url = uploaded.FileUrl,
                    FileSize = uploaded.FileSize,
                    ImageCount = context.ImageCount + 1,
                    MaxImageCount = MaxImagesPerMemory,
                    StorageUsedBytes = usage.StorageUsedBytes,
                    StorageLimitBytes = usage.StorageLimitBytes
                }
                : new OutputMemoryImageAction
                {
                    ImageId = commit.Data.ImageId,
                    Url = commit.Data.Url,
                    FileSize = commit.Data.FileSize,
                    ImageCount = commit.Data.ImageCount,
                    MaxImageCount = commit.Data.MaxImageCount,
                    StorageUsedBytes = usage.StorageUsedBytes,
                    StorageLimitBytes = usage.StorageLimitBytes
                };
        }
        catch
        {
            await SafeDeletePhysicalAsync(uploaded.StorageKey, cancellationToken);
            throw;
        }
    }

    private async Task DeletePhysicalImagesAsync(
        long familyId,
        long memoryId,
        IEnumerable<OutputMemoryImageItem> images,
        CancellationToken cancellationToken)
    {
        foreach (var image in images)
        {
            if (string.IsNullOrWhiteSpace(image.StorageKey))
            {
                continue;
            }

            try
            {
                if (await _files.ExistsAsync(image.StorageKey, cancellationToken))
                {
                    await _files.DeleteAsync(image.StorageKey, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete physical memory image. FamilyId={FamilyId}, MemoryId={MemoryId}, ImageId={ImageId}, StorageKey={StorageKey}",
                    familyId,
                    memoryId,
                    image.Id,
                    image.StorageKey);
                throw new BadRequestException(
                    "Unable to delete the image file. Please try again.",
                    ex.Message);
            }
        }
    }

    private async Task SafeDeletePhysicalAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await _files.DeleteAsync(storageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to roll back physical file after image commit failure. StorageKey={StorageKey}",
                storageKey);
        }
    }

    private async Task SafeDeleteMemoryAsync(
        long familyId,
        long memoryId,
        string username,
        CancellationToken cancellationToken)
    {
        try
        {
            var memory = await _memories.GetByIdAsync(new InputGetMemory
            {
                FamilyId = familyId,
                Id = memoryId
            });
            if (memory is not null)
            {
                await DeletePhysicalImagesAsync(familyId, memoryId, memory.Images, cancellationToken);
            }

            await _memories.SoftDeleteAsync(new InputDeleteMemory
            {
                FamilyId = familyId,
                Id = memoryId,
                CancelledBy = username
            });
        }
        catch
        {
            // Best-effort rollback when image attach fails after insert.
        }
    }

    private static string NormalizeSort(string? sortBy)
    {
        var value = string.IsNullOrWhiteSpace(sortBy) ? "recent" : sortBy.Trim().ToLowerInvariant();
        return AllowedSort.Contains(value) ? value : "recent";
    }
}
