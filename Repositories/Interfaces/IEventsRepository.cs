using MidnightApi.Models;

namespace MidnightApi.Repositories.Interfaces;

public interface IEventsRepository
{
    Task<OutputPagedEvents?> GetListAsync(InputEventList input);
    Task<OutputGetEvent?> GetByIdAsync(InputGetEvent input);
    Task<OutputCreateEvent> CreateAsync(InputCreateEvent input);
    Task<OutputUpdateEvent> UpdateAsync(InputUpdateEvent input);
    Task<OutputDeleteEvent> SoftDeleteAsync(InputDeleteEvent input);
    Task<OutputEventCoverAction> CommitCoverAsync(InputEventCoverCommit input);
    Task<OutputEventCoverAction> RemoveCoverAsync(InputEventCoverRemove input);
}
