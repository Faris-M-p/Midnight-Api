using Dapper;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Api;
using Npgsql;

namespace MidnightApi.Repositories;

public class UserAccountsRepository : IUserAccountsRepository
{
    private readonly IDbConnectionFactory _connections;

    public UserAccountsRepository(IDbConnectionFactory connections)
    {
        _connections = connections;
    }

    public async Task<OutputGetAccount?> GetByIdAsync(long id)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<OutputGetAccount>(
            StoredProcedures.AccountSelect,
            new { p_id = id });
    }

    public async Task<(OutputGetAccount Account, string PasswordHash)?> GetLoginByUsernameAsync(string username)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<AccountLoginRow>(
            StoredProcedures.AccountLogin,
            new { p_username = username });

        if (row is null)
        {
            return null;
        }

        return (new OutputGetAccount
        {
            ID_UserAccounts = row.ID_UserAccounts,
            FK_Families = row.FK_Families,
            Username = row.Username,
            Email = row.Email,
            IsActive = row.IsActive
        }, row.PasswordHash);
    }

    public async Task<OutputGetAccount> CreateAsync(InputCreateAccount input, string createdBy)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.QuerySingleAsync<OutputGetAccount>(
            StoredProcedures.AccountRegister,
            new
            {
                p_fk_families = input.FK_Families,
                p_username = input.Username,
                p_email = input.Email,
                p_password_hash = input.PasswordHash,
                p_is_active = input.IsActive,
                p_created_by = createdBy
            });
    }

    public async Task<OutputGetAccount?> UpdateAsync(long id, InputUpdateAccount input, string updatedBy)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<OutputGetAccount>(
            StoredProcedures.AccountUpdate,
            new
            {
                p_id = id,
                p_username = input.Username,
                p_email = input.Email,
                p_password_hash = input.PasswordHash,
                p_updated_by = updatedBy
            });
    }

    public async Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            StoredProcedures.AccountExistsByUsername,
            new { p_username = username, p_exclude_id = excludeId });
    }

    private sealed class AccountLoginRow
    {
        public long ID_UserAccounts { get; set; }
        public long FK_Families { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
    }
}
