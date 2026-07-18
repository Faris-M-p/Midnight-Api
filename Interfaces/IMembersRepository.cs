namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IMembersRepository
{
    Task<OutputPagedMembers> GetListAsync(long familyId, InputMemberListQuery query);
    Task<OutputMemberProfile?> GetProfileAsync(long familyId, long memberId);
    Task<OutputMemberProfile> CreateAsync(long familyId, InputCreateMember input, string createdBy);
    Task<OutputMemberProfile?> UpdateAsync(long familyId, long memberId, InputUpdateMember input, string updatedBy);
    Task<bool> SoftDeleteAsync(long familyId, long memberId, string deletedBy);
    Task MapSpouseAsync(long familyId, long memberId, long spouseId, string updatedBy);
    Task<OutputDashboard> GetDashboardAsync(long familyId);
    Task<List<OutputTimelineItem>> GetTimelineAsync(long familyId);
    Task<bool> ExistsInFamilyAsync(long familyId, long memberId);
    Task<bool> HasRootMemberAsync(long familyId, long? excludeMemberId = null);
    Task<long?> GetParentIdAsync(long memberId);
    Task<(long? ParentId, long? SpouseId)?> GetRelationAsync(long familyId, long memberId);
}
