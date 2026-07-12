
namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IMemberImagesRepository
{
    Task<List<OutputGetMemberImage>> GetAllAsync();
    Task<OutputGetMemberImage?> GetByIdAsync(long id);
    Task<List<OutputGetMemberImage>> GetByMemberIdAsync(long memberId);
    Task<OutputGetMemberImage> CreateAsync(InputCreateMemberImage input, string createdBy);
    Task<OutputGetMemberImage?> UpdateAsync(long id, InputUpdateMemberImage input, string updatedBy);
    Task<bool> SoftDeleteAsync(long id, string deletedBy);
}
