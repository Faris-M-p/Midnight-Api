
namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IUserAccountsRepository
{
    Task<List<OutputGetAccount>> GetAllAsync();
    Task<OutputGetAccount?> GetByIdAsync(long id);
    Task<OutputGetAccount?> GetByMemberIdAsync(long memberId);
    Task<OutputGetAccount> CreateAsync(InputCreateAccount input, string createdBy);
    Task<OutputGetAccount?> UpdateAsync(long id, InputUpdateAccount input, string updatedBy);
    Task<bool> SoftDeleteAsync(long id, string deletedBy);
    Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null);
}
