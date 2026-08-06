using Dapper;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Api;
using Npgsql;

namespace MidnightApi.Repositories;

public class FamiliesRepository : IFamiliesRepository
{
    private readonly IDbConnectionFactory _connections;

    public FamiliesRepository(IDbConnectionFactory connections)
    {
        _connections = connections;
    }

    public async Task<OutputGetFamily?> GetByIdAsync(long id)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<OutputGetFamily>(
            StoredProcedures.FamilySelect,
            new { p_id = id });
    }

    public async Task<OutputGetFamily> CreateAsync(InputCreateFamily input, string createdBy)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.QuerySingleAsync<OutputGetFamily>(
            StoredProcedures.FamilyInsert,
            new
            {
                p_family_code = input.FamilyCode,
                p_family_name = input.FamilyName,
                p_description = input.Description,
                p_created_by = createdBy
            });
    }

    public async Task<OutputGetFamily?> UpdateAsync(long id, InputUpdateFamily input, string updatedBy)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<OutputGetFamily>(
            StoredProcedures.FamilyUpdate,
            new
            {
                p_id = id,
                p_family_name = input.FamilyName,
                p_description = input.Description,
                p_updated_by = updatedBy
            });
    }

    public async Task<bool> ExistsByCodeAsync(string code, long? excludeFamilyId = null)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            StoredProcedures.FamilyExistsByCode,
            new { p_code = code, p_exclude_family_id = excludeFamilyId });
    }
}
