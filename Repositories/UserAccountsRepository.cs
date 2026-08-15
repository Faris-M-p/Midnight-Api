using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Repositories;

public class UserAccountsRepository : IUserAccountsRepository
{
    public readonly IDataAccessDapper _dataAccessDapper;
    private readonly IFamilyCodeGenerator _familyCodes;

    public UserAccountsRepository(IDataAccessDapper dataAccessDapper, IFamilyCodeGenerator familyCodes)
    {
        _dataAccessDapper = dataAccessDapper;
        _familyCodes = familyCodes;
    }

    public Task<OutputGetAccount?> GetByIdAsync(InputGetAccount input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputGetAccount>(
            StoredProcedures.AccountSelect, input);

    public Task<OutputLoginAccount?> GetLoginByUsernameAsync(InputLoginAccount input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputLoginAccount>(
            StoredProcedures.AccountLogin, input);

    public Task<OutputLoginAccount?> GetByEmailAsync(InputGetAccountByEmail input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputLoginAccount>(
            StoredProcedures.AccountSelectByEmail, input);

    public async Task<OutputRegister> RegisterAsync(InputRegisterAccount input)
    {
        OutputRegister result = new();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (string.IsNullOrWhiteSpace(input.FamilyCode) || attempt > 0)
            {
                input.FamilyCode = _familyCodes.Generate(input.FamilyName);
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

    public Task<OutputUpdateAccount> SetEmailVerifiedAsync(InputSetEmailVerified input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateAccount>(
            StoredProcedures.AccountSetEmailVerified, input);

    public Task<OutputUpdateAccount> UpdatePasswordAsync(InputUpdateAccountPassword input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateAccount>(
            StoredProcedures.AccountUpdatePassword, input);

    private static bool IsFamilyCodeConflict(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && message.Contains("Family code already exists", StringComparison.OrdinalIgnoreCase);
}
