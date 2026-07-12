
namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IMemberAddressesRepository
{
    Task<List<OutputGetMemberAddress>> GetAllAsync();
    Task<OutputGetMemberAddress?> GetByIdAsync(long id);
    Task<List<OutputGetMemberAddress>> GetByMemberIdAsync(long memberId);
    Task<OutputGetMemberAddress> CreateAsync(InputCreateMemberAddress input, string createdBy);
    Task<OutputGetMemberAddress?> UpdateAsync(long id, InputUpdateMemberAddress input, string updatedBy);
    Task<bool> SoftDeleteAsync(long id, string deletedBy);
}
