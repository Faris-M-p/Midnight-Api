
namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IMemberSocialLinksRepository
{
    Task<List<OutputGetMemberSocialLink>> GetAllAsync();
    Task<OutputGetMemberSocialLink?> GetByIdAsync(long id);
    Task<List<OutputGetMemberSocialLink>> GetByMemberIdAsync(long memberId);
    Task<OutputGetMemberSocialLink> CreateAsync(InputCreateMemberSocialLink input, string createdBy);
    Task<OutputGetMemberSocialLink?> UpdateAsync(long id, InputUpdateMemberSocialLink input, string updatedBy);
    Task<bool> SoftDeleteAsync(long id, string deletedBy);
}
