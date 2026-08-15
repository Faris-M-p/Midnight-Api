using MidnightApi.Models;

namespace MidnightApi.Repositories.Interfaces;

public interface IMembersRepository
{
    Task<OutputPagedMembers?> GetListAsync(InputMemberList input);
    Task<OutputFamilyTree?> GetTreeAsync(InputMemberTree input);
    Task<OutputGetMember?> GetByIdAsync(InputGetMember input);
    Task<OutputCreateMember> CreateAsync(InputCreateMember input);
    Task<OutputUpdateMember> UpdateAsync(InputUpdateMember input);
    Task<OutputDeleteMember> SoftDeleteAsync(InputDeleteMember input);
    Task<OutputMapSpouse> MapSpouseAsync(InputMapSpouse input);
    Task<OutputDashboard?> GetDashboardAsync(InputMemberDashboard input);
    Task<List<OutputTimelineItem>> GetTimelineAsync(InputMemberTimeline input);
    Task<bool> IsInScopeBranchAsync(InputMemberScopeCheck input);
}
