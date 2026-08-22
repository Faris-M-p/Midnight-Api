using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Repositories;

public class FamiliesRepository : IFamiliesRepository
{
    public readonly IDataAccessDapper _iDataAccessDapper;
    private readonly IFamilyCodeGenerator _iFamilyCodeGenerator;

    public FamiliesRepository(IDataAccessDapper dataAccessDapper, IFamilyCodeGenerator familyCodes)
    {
        _iDataAccessDapper = dataAccessDapper;
        _iFamilyCodeGenerator = familyCodes;
    }

    public Task<OutputGetFamily?> GetByIdAsync(InputGetFamily input) =>
        _iDataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputGetFamily>(
            StoredProcedures.FamilySelect, input);

    public Task<OutputFamilyByCode?> GetByCodeAsync(InputGetFamilyByCode input) =>
        _iDataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputFamilyByCode>(
            StoredProcedures.FamilySelectByCode, input);

    public async Task<OutputCreateFamily> CreateAsync(InputCreateFamily input)
    {
        OutputCreateFamily result = new();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (string.IsNullOrWhiteSpace(input.FamilyCode) || attempt > 0)
            {
                input.FamilyCode = _iFamilyCodeGenerator.Generate(input.FamilyName);
            }

            result = await _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputCreateFamily>(
                StoredProcedures.FamilyInsert, input);

            if (result.Status || !IsFamilyCodeConflict(result.ResponseMessage))
            {
                return result;
            }
        }

        return result;
    }

    public Task<OutputUpdateFamily> UpdateAsync(InputUpdateFamily input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateFamily>(
            StoredProcedures.FamilyUpdate, input);

    public Task<OutputUpdateFamily> UpdateCoverAsync(InputUpdateFamilyCover input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateFamily>(
            StoredProcedures.FamilyUpdateCover, input);

    public Task<OutputFamilyStorageSums?> GetStorageSumsAsync(InputFamilyStorage input) =>
        _iDataAccessDapper.GetPayloadByStoredProcedureAsync<OutputFamilyStorageSums>(
            StoredProcedures.FamilyStorageSelect, input);

    private static bool IsFamilyCodeConflict(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && message.Contains("Family code already exists", StringComparison.OrdinalIgnoreCase);
}
