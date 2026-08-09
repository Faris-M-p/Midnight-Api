using MidnightApi.Data;
using MidnightApi.DataAccess;
using MidnightApi.Interfaces;
using MidnightApi.Models;
using MidnightApi.Services;

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

    public async Task<OutputRegister> RegisterAsync(InputRegisterAccount input)
    {
        OutputRegister result = new();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (string.IsNullOrWhiteSpace(input.FamilyCode) || attempt > 0)
            {
                input.FamilyCode = FamilyCodeGenerator.Generate(input.FamilyName);
            }

            result = await _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputRegister>(
                StoredProcedures.AccountRegister, input);

            if (result.Status || !IsFamilyCodeConflict(result.ResponseMessage))
            {
                return result;
            }
        }

        return result;
    }

    public Task<OutputUpdateAccount> UpdateAsync(InputUpdateAccount input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateAccount>(
            StoredProcedures.AccountUpdate, input);

    private static bool IsFamilyCodeConflict(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && message.Contains("Family code already exists", StringComparison.OrdinalIgnoreCase);
}
