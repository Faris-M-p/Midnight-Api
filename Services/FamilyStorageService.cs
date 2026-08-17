using Microsoft.Extensions.Options;
using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Options;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Services;

public class FamilyStorageService : IFamilyStorageService
{
    private readonly IDataAccessDapper _dataAccess;
    private readonly StorageOptions _options;

    public FamilyStorageService(
        IDataAccessDapper dataAccess,
        IOptions<StorageOptions> options)
    {
        _dataAccess = dataAccess;
        _options = options.Value;
    }

    public async Task<OutputFamilyStorageUsage> GetStorageUsageAsync(long familyId)
    {
        var sums = await _dataAccess.GetPayloadByStoredProcedureAsync<OutputFamilyStorageSums>(
            StoredProcedures.FamilyStorageSelect,
            new InputFamilyStorage { FamilyId = familyId });

        if (sums is null)
        {
            throw new NotFoundException("Family not found.");
        }

        return BuildUsage(sums);
    }

    public async Task EnsureCanUploadAsync(long familyId, long additionalBytes)
    {
        if (additionalBytes < 0)
        {
            throw new BadRequestException("File size is invalid.");
        }

        var usage = await GetStorageUsageAsync(familyId);
        if (usage.StorageUsedBytes + additionalBytes > usage.StorageLimitBytes)
        {
            throw new StorageLimitReachedException(
                "Family storage limit reached. Free up space or remove unused images before uploading.");
        }
    }

    private OutputFamilyStorageUsage BuildUsage(OutputFamilyStorageSums sums)
    {
        var limit = _options.FamilyLimitBytes > 0
            ? _options.FamilyLimitBytes
            : 5L * 1024 * 1024 * 1024;
        var used = Math.Max(0, sums.StorageUsedBytes);
        var remaining = Math.Max(0, limit - used);
        var percentage = limit <= 0 ? 0 : Math.Round(used * 100.0 / limit, 2);

        return new OutputFamilyStorageUsage
        {
            FamilyId = sums.FamilyId,
            StorageUsedBytes = used,
            MemberImagesStorageBytes = Math.Max(0, sums.MemberImagesStorageBytes),
            MemoryImagesStorageBytes = Math.Max(0, sums.MemoryImagesStorageBytes),
            StorageLimitBytes = limit,
            RemainingBytes = remaining,
            UsagePercentage = percentage
        };
    }
}
