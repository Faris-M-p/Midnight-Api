using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Repositories;

public class UserAccountsRepository : IUserAccountsRepository
{
    public readonly IDataAccessDapper _iDataAccessDapper;
    private readonly IFamilyCodeGenerator _iFamilyCodeGenerator;

    public UserAccountsRepository(IDataAccessDapper dataAccessDapper, IFamilyCodeGenerator familyCodes)
    {
        _iDataAccessDapper = dataAccessDapper;
        _iFamilyCodeGenerator = familyCodes;
    }

    public Task<OutputGetAccount?> GetByIdAsync(InputGetAccount input) =>
        _iDataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputGetAccount>(
            StoredProcedures.AccountSelect, input);

    public Task<OutputLoginAccount?> GetLoginByUsernameAsync(InputLoginAccount input) =>
        _iDataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputLoginAccount>(
            StoredProcedures.AccountLogin, input);

    public Task<OutputLoginAccount?> GetByEmailAsync(InputGetAccountByEmail input) =>
        _iDataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputLoginAccount>(
            StoredProcedures.AccountSelectByEmail, input);

    public async Task<OutputRegister> RegisterAsync(InputRegisterAccount input)
    {
        OutputRegister result = new();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (string.IsNullOrWhiteSpace(input.FamilyCode) || attempt > 0)
            {
                input.FamilyCode = _iFamilyCodeGenerator.Generate(input.FamilyName);
            }

            result = await _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputRegister>(
                StoredProcedures.AccountRegister, input);

            if (result.Status || !IsFamilyCodeConflict(result.ResponseMessage))
            {
                return result;
            }
        }

        return result;
    }

    public Task<OutputUpdateAccount> UpdateAsync(InputUpdateAccount input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateAccount>(
            StoredProcedures.AccountUpdate, input);

    public Task<OutputUpdateAccount> SetEmailVerifiedAsync(InputSetEmailVerified input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateAccount>(
            StoredProcedures.AccountSetEmailVerified, input);

    public Task<OutputUpdateAccount> UpdatePasswordAsync(InputUpdateAccountPassword input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateAccount>(
            StoredProcedures.AccountUpdatePassword, input);

    private static bool IsFamilyCodeConflict(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && message.Contains("Family code already exists", StringComparison.OrdinalIgnoreCase);
}
