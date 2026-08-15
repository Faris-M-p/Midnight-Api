using MidnightApi.Models;

namespace MidnightApi.Repositories.Interfaces;

public interface IUserAccountsRepository
{
    Task<OutputGetAccount?> GetByIdAsync(InputGetAccount input);
    Task<OutputLoginAccount?> GetLoginByUsernameAsync(InputLoginAccount input);
    Task<OutputLoginAccount?> GetByEmailAsync(InputGetAccountByEmail input);
    Task<OutputRegister> RegisterAsync(InputRegisterAccount input);
    Task<OutputUpdateAccount> UpdateAsync(InputUpdateAccount input);
    Task<OutputUpdateAccount> SetEmailVerifiedAsync(InputSetEmailVerified input);
    Task<OutputUpdateAccount> UpdatePasswordAsync(InputUpdateAccountPassword input);
}
