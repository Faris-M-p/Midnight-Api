namespace MidnightApi.Interfaces;

using MidnightApi.Models;

public interface IUserAccountsRepository
{
    Task<OutputGetAccount?> GetByIdAsync(InputGetAccount input);
    Task<OutputLoginAccount?> GetLoginByUsernameAsync(InputLoginAccount input);
    Task<OutputRegister> RegisterAsync(InputRegisterAccount input);
    Task<OutputUpdateAccount> UpdateAsync(InputUpdateAccount input);
}
