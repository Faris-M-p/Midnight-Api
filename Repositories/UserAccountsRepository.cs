using MidnightApi.Data;
using MidnightApi.DataAccess;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Repositories;

public class UserAccountsRepository : IUserAccountsRepository
{
    public readonly IDataAccessDapper _dataAccessDapper;

    public UserAccountsRepository(IDataAccessDapper dataAccessDapper)
    {
        _dataAccessDapper = dataAccessDapper;
    }

    public Task<OutputGetAccount?> GetByIdAsync(InputGetAccount input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputGetAccount>(
            StoredProcedures.AccountSelect, input);

    public Task<OutputLoginAccount?> GetLoginByUsernameAsync(InputLoginAccount input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputLoginAccount>(
            StoredProcedures.AccountLogin, input);

    public Task<OutputRegister> RegisterAsync(InputRegisterAccount input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputRegister>(
            StoredProcedures.AccountRegister, input);

    public Task<OutputUpdateAccount> UpdateAsync(InputUpdateAccount input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateAccount>(
            StoredProcedures.AccountUpdate, input);
}
