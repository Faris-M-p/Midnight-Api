using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;

namespace MidnightApi.Repositories;

public class EventsRepository : IEventsRepository
{
    private static readonly HashSet<string> AllowedSort = new(StringComparer.OrdinalIgnoreCase)
    {
        "date",
        "recent",
        "title"
    };

    private readonly IDataAccessDapper _iDataAccessDapper;

    public EventsRepository(IDataAccessDapper dataAccessDapper)
    {
        _iDataAccessDapper = dataAccessDapper;
    }

    public async Task<OutputPagedEvents?> GetListAsync(InputEventList input)
    {
        input.SortBy = NormalizeSort(input.SortBy);
        input.Page = Math.Max(input.Page, 1);
        input.PageSize = Math.Clamp(input.PageSize <= 0 ? 100 : input.PageSize, 1, 200);

        var (items, totalCount) = await _iDataAccessDapper.GetPagedListByStoredProcedureAsync<OutputEventListItem>(
            StoredProcedures.EventList, input);

        return new OutputPagedEvents
        {
            Items = items,
            Page = input.Page,
            PageSize = input.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)input.PageSize)
        };
    }

    public Task<OutputGetEvent?> GetByIdAsync(InputGetEvent input) =>
        _iDataAccessDapper.GetPayloadByStoredProcedureAsync<OutputGetEvent>(
            StoredProcedures.EventSelect, input);

    public Task<OutputCreateEvent> CreateAsync(InputCreateEvent input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputCreateEvent>(
            StoredProcedures.EventInsert, input);

    public Task<OutputUpdateEvent> UpdateAsync(InputUpdateEvent input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateEvent>(
            StoredProcedures.EventUpdate, input);

    public Task<OutputDeleteEvent> SoftDeleteAsync(InputDeleteEvent input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputDeleteEvent>(
            StoredProcedures.EventDelete, input);

    public Task<OutputEventCoverAction> CommitCoverAsync(InputEventCoverCommit input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputEventCoverAction>(
            StoredProcedures.EventCoverCommit, input);

    public Task<OutputEventCoverAction> RemoveCoverAsync(InputEventCoverRemove input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputEventCoverAction>(
            StoredProcedures.EventCoverRemove, input);

    private static string NormalizeSort(string? sortBy)
    {
        var value = string.IsNullOrWhiteSpace(sortBy) ? "date" : sortBy.Trim().ToLowerInvariant();
        return AllowedSort.Contains(value) ? value : "date";
    }
}
