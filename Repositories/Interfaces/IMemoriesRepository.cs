using MidnightApi.Models;

namespace MidnightApi.Repositories.Interfaces;

public interface IMemoriesRepository
{
    Task<OutputPagedMemories?> GetListAsync(InputMemoryList input);
    Task<OutputGetMemory?> GetByIdAsync(InputGetMemory input);
    Task<OutputCreateMemory> CreateAsync(InputCreateMemory input);
    Task<OutputUpdateMemory> UpdateAsync(InputUpdateMemory input);
    Task<OutputDeleteMemory> SoftDeleteAsync(InputDeleteMemory input);
    Task<OutputMemoryImageUploadContext?> GetImageUploadContextAsync(InputMemoryImageUploadContext input);
    Task<OutputMemoryImageCommit> CommitImageAsync(InputMemoryImageCommit input);
    Task<OutputMemoryImageDetail?> GetImageAsync(InputMemoryImageGet input);
    Task<OutputMemoryImageDelete> SoftDeleteImageAsync(InputMemoryImageDelete input);
    Task<OutputMemoryCoverSet> SetCoverAsync(InputMemoryCoverSet input);
}
