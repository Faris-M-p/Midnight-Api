using MidnightApi.Data;
using MidnightApi.DataAccess;
using MidnightApi.Interfaces;
using MidnightApi.Models;
using MidnightApi.Services;

namespace MidnightApi.Repositories;

public class FamiliesRepository : IFamiliesRepository
{
    public readonly IDataAccessDapper _dataAccessDapper;

    public FamiliesRepository(IDataAccessDapper dataAccessDapper)
    {
        _dataAccessDapper = dataAccessDapper;
    }

    public Task<OutputGetFamily?> GetByIdAsync(InputGetFamily input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputGetFamily>(
            StoredProcedures.FamilySelect, input);

    public async Task<OutputCreateFamily> CreateAsync(InputCreateFamily input)
    {
        OutputCreateFamily result = new();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (string.IsNullOrWhiteSpace(input.FamilyCode) || attempt > 0)
            {
                input.FamilyCode = FamilyCodeGenerator.Generate(input.FamilyName);
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

    private static bool IsFamilyCodeConflict(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && message.Contains("Family code already exists", StringComparison.OrdinalIgnoreCase);
}
