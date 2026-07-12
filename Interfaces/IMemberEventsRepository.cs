
namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IMemberEventsRepository
{
    Task<List<OutputGetMemberEvent>> GetAllAsync();
    Task<OutputGetMemberEvent?> GetByIdAsync(long id);
    Task<List<OutputGetMemberEvent>> GetByMemberIdAsync(long memberId);
    Task<OutputGetMemberEvent> CreateAsync(InputCreateMemberEvent input, string createdBy);
    Task<OutputGetMemberEvent?> UpdateAsync(long id, InputUpdateMemberEvent input, string updatedBy);
    Task<bool> SoftDeleteAsync(long id, string deletedBy);
}
