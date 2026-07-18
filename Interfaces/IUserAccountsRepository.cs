namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IUserAccountsRepository
{
    Task<OutputGetAccount?> GetByIdAsync(long id);
    Task<(OutputGetAccount Account, string PasswordHash)?> GetLoginByUsernameAsync(string username);
    Task<OutputGetAccount> CreateAsync(InputCreateAccount input, string createdBy);
    Task<OutputGetAccount?> UpdateAsync(long id, InputUpdateAccount input, string updatedBy);
    Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null);
}
