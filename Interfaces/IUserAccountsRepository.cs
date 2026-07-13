namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IUserAccountsRepository
{
    Task<OutputGetAccount?> GetByIdAsync(long id);
    Task<OutputGetAccount?> GetByFamilyIdAsync(long familyId);
    Task<(OutputGetAccount Account, string PasswordHash)?> GetLoginByUsernameAsync(string username);
    Task<OutputGetAccount> CreateAsync(InputCreateAccount input, string createdBy);
    Task<OutputGetAccount?> UpdateAsync(long id, InputUpdateAccount input, string updatedBy);
    Task<bool> SoftDeleteAsync(long id, string deletedBy);
    Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null);
    Task<bool> ExistsByFamilyIdAsync(long familyId);
}
