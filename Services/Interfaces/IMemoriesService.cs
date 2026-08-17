using MidnightApi.Models;

namespace MidnightApi.Services.Interfaces;

public interface IMemoriesService
{
    Task<OutputPagedMemories> ListAsync(long familyId, InputMemoryListQueryView query);
    Task<OutputGetMemory> GetAsync(long familyId, long memoryId);
    Task<OutputGetMemory> CreateAsync(long familyId, string username, InputCreateMemoryView request, CancellationToken cancellationToken = default);
    Task<OutputGetMemory> UpdateAsync(long familyId, long memoryId, string username, InputUpdateMemoryView request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long familyId, long memoryId, string username, CancellationToken cancellationToken = default);
    Task<OutputMemoryImageAction> UploadImageAsync(long familyId, long memoryId, string username, InputUploadMemoryImageView request, CancellationToken cancellationToken = default);
    Task<OutputMemoryImageAction> DeleteImageAsync(long familyId, long memoryId, long imageId, string username, CancellationToken cancellationToken = default);
    Task<OutputGetMemory> SetCoverAsync(long familyId, long memoryId, long imageId, string username);
}
