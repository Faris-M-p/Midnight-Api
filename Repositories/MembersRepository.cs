using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;

namespace MidnightApi.Repositories;

public class MembersRepository : IMembersRepository
{
    public readonly IDataAccessDapper _iDataAccessDapper;

    public MembersRepository(IDataAccessDapper dataAccessDapper)
    {
        _iDataAccessDapper = dataAccessDapper;
    }

    public async Task<OutputPagedMembers?> GetListAsync(InputMemberList input)
    {
        var (items, totalCount) = await _iDataAccessDapper.GetPagedListByStoredProcedureAsync<OutputMemberListItem>(
            StoredProcedures.MemberList, input);

        var page = Math.Max(input.Page, 1);
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);

        return new OutputPagedMembers
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public Task<OutputFamilyTree?> GetTreeAsync(InputMemberTree input) =>
        _iDataAccessDapper.GetPayloadByStoredProcedureAsync<OutputFamilyTree>(
            StoredProcedures.MemberTree, input);

    public Task<OutputGetMember?> GetByIdAsync(InputGetMember input) =>
        _iDataAccessDapper.GetPayloadByStoredProcedureAsync<OutputGetMember>(
            StoredProcedures.MemberSelect, input);

    public Task<OutputCreateMember> CreateAsync(InputCreateMember input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputCreateMember>(
            StoredProcedures.MemberInsert, input);

    public Task<OutputUpdateMember> UpdateAsync(InputUpdateMember input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateMember>(
            StoredProcedures.MemberUpdate, input);

    public Task<OutputDeleteMember> SoftDeleteAsync(InputDeleteMember input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputDeleteMember>(
            StoredProcedures.MemberDelete, input);

    public Task<OutputMapSpouse> MapSpouseAsync(InputMapSpouse input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputMapSpouse>(
            StoredProcedures.MemberMapSpouse, input);

    public Task<OutputDashboard?> GetDashboardAsync(InputMemberDashboard input) =>
        _iDataAccessDapper.GetPayloadByStoredProcedureAsync<OutputDashboard>(
            StoredProcedures.MemberDashboard, input);

    public Task<List<OutputTimelineItem>> GetTimelineAsync(InputMemberTimeline input) =>
        _iDataAccessDapper.GetListByStoredProcedureAsync<OutputTimelineItem>(
            StoredProcedures.MemberTimeline, input);

    public async Task<bool> IsInScopeBranchAsync(InputMemberScopeCheck input)
    {
        var row = await _iDataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputMemberScopeCheck>(
            StoredProcedures.MemberIsInScopeBranch, input);
        return row?.IsInBranch == true;
    }
}
