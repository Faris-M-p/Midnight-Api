using MidnightApi.Data;
using MidnightApi.DataAccess;
using MidnightApi.Interfaces;
using MidnightApi.Models;

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

    public Task<OutputCreateFamily> CreateAsync(InputCreateFamily input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputCreateFamily>(
            StoredProcedures.FamilyInsert, input);

    public Task<OutputUpdateFamily> UpdateAsync(InputUpdateFamily input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateFamily>(
            StoredProcedures.FamilyUpdate, input);
}
