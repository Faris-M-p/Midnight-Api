using MidnightApi.DataAccess;

namespace MidnightApi.Models;

public class InputFamilyStorage
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }
}

/// <summary>
/// Raw SUM payload from ProFamilyStorageSelect (limit applied in FamilyStorageService).
/// </summary>
public class OutputFamilyStorageSums
{
    public long FamilyId { get; set; }
    public long MemberImagesStorageBytes { get; set; }
    public long MemoryImagesStorageBytes { get; set; }
    public long StorageUsedBytes { get; set; }
}

public class OutputFamilyStorageUsage
{
    public long FamilyId { get; set; }
    public long StorageUsedBytes { get; set; }
    public long MemberImagesStorageBytes { get; set; }
    public long MemoryImagesStorageBytes { get; set; }
    public long StorageLimitBytes { get; set; }
    public long RemainingBytes { get; set; }
    public double UsagePercentage { get; set; }
}
