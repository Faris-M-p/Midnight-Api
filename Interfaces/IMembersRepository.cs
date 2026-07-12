
namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IMembersRepository
{
    Task<List<OutputGetMember>> GetAllAsync();
    Task<OutputGetMember?> GetByIdAsync(long id);
    Task<OutputGetMemberProfile?> GetByIdWithDetailsAsync(long id);
    Task<List<OutputGetMember>> GetByFamilyIdAsync(InputGetFamilyMembers input);
    Task<OutputGetMember?> GetRootByFamilyIdAsync(long familyId);
    Task<OutputGetMemberTree?> GetRootWithTreeDataAsync(InputGetMemberTree input);
    Task<List<OutputGetMember>> SearchByNameAsync(InputSearchMembers input);
    Task<List<OutputGetMember>> GetByGenerationAsync(InputGetMembersByGeneration input);
    Task<OutputGetMember> CreateAsync(InputCreateMember input, string createdBy);
    Task<OutputGetMember?> UpdateAsync(long id, InputUpdateMember input, string updatedBy);
    Task<bool> SoftDeleteAsync(long id, string deletedBy);
    Task<bool> HasRootMemberAsync(long familyId, long? excludeMemberId = null);
    Task<long?> GetParentIdAsync(long memberId);
    Task LinkSpouseAsync(long memberId, long spouseId, string updatedBy);
}
