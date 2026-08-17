using MidnightApi.Models;

namespace MidnightApi.Services.Interfaces;

public interface IFamilyStorageService
{
    Task<OutputFamilyStorageUsage> GetStorageUsageAsync(long familyId);

    /// <summary>
    /// Ensures current usage + additionalBytes does not exceed the configured family limit.
    /// Throws <see cref="Exceptions.StorageLimitReachedException"/> when exceeded.
    /// </summary>
    Task EnsureCanUploadAsync(long familyId, long additionalBytes);
}
