
namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IMemberNotesRepository
{
    Task<List<OutputGetMemberNote>> GetAllAsync();
    Task<OutputGetMemberNote?> GetByIdAsync(long id);
    Task<List<OutputGetMemberNote>> GetByMemberIdAsync(long memberId);
    Task<OutputGetMemberNote> CreateAsync(InputCreateMemberNote input, string createdBy);
    Task<OutputGetMemberNote?> UpdateAsync(long id, InputUpdateMemberNote input, string updatedBy);
    Task<bool> SoftDeleteAsync(long id, string deletedBy);
}
