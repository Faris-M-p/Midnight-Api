using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Repositories;

public class FamiliesRepository : IFamiliesRepository
{
    public readonly IDataAccessDapper _dataAccessDapper;
    private readonly IFamilyCodeGenerator _familyCodes;

    public FamiliesRepository(IDataAccessDapper dataAccessDapper, IFamilyCodeGenerator familyCodes)
    {
        _dataAccessDapper = dataAccessDapper;
        _familyCodes = familyCodes;
    }

    public Task<OutputGetFamily?> GetByIdAsync(InputGetFamily input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputGetFamily>(
            StoredProcedures.FamilySelect, input);

    public Task<OutputFamilyByCode?> GetByCodeAsync(InputGetFamilyByCode input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputFamilyByCode>(
            StoredProcedures.FamilySelectByCode, input);

    public async Task<OutputCreateFamily> CreateAsync(InputCreateFamily input)
    {
        OutputCreateFamily result = new();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (string.IsNullOrWhiteSpace(input.FamilyCode) || attempt > 0)
            {
                input.FamilyCode = _familyCodes.Generate(input.FamilyName);
            }

            result = await _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputCreateFamily>(
                StoredProcedures.FamilyInsert, input);

            if (result.Status || !IsFamilyCodeConflict(result.ResponseMessage))
            {
                return result;
            }
        }

        return result;
    }

    public Task<OutputUpdateFamily> UpdateAsync(InputUpdateFamily input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateFamily>(
            StoredProcedures.FamilyUpdate, input);

    public Task<OutputUpdateFamily> UpdateCoverAsync(InputUpdateFamilyCover input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateFamily>(
            StoredProcedures.FamilyUpdateCover, input);

    private static bool IsFamilyCodeConflict(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && message.Contains("Family code already exists", StringComparison.OrdinalIgnoreCase);
}
