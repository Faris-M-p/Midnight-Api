using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;

namespace MidnightApi.Repositories;

public class EventsRepository : IEventsRepository
{
    private readonly IDataAccessDapper _iDataAccessDapper;

    public EventsRepository(IDataAccessDapper dataAccessDapper)
    {
        _iDataAccessDapper = dataAccessDapper;
    }

    public async Task<OutputPagedEvents?> GetListAsync(InputEventList input)
    {
        var (items, totalCount) = await _iDataAccessDapper.GetPagedListByStoredProcedureAsync<OutputEventListItem>(
            StoredProcedures.EventList, input);

        var page = Math.Max(input.Page, 1);
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 100 : input.PageSize, 1, 200);

        return new OutputPagedEvents
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
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
}
