using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Repositories.Managers;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class UserAccountsRepository : IUserAccountsRepository
{
    private readonly DbConnectionClass _db;
    private readonly UserAccountsRepositoryManager _manager;

    public UserAccountsRepository(DbConnectionClass db, UserAccountsRepositoryManager manager)
    {
        _db = db;
        _manager = manager;
    }

    public async Task<OutputGetAccount?> GetByIdAsync(long id)
    {
        var account = await _manager.LoadActiveByIdAsync(id);
        return account is null ? null : _manager.MapToOutput(account);
    }

    public async Task<(OutputGetAccount Account, string PasswordHash)?> GetLoginByUsernameAsync(string username)
    {
        var account = await _manager.LoadActiveByUsernameAsync(username);
        return account is null ? null : (_manager.MapToOutput(account), account.PasswordHash);
    }

    public async Task<OutputGetAccount> CreateAsync(InputCreateAccount input, string createdBy)
    {
        var account = _manager.BuildCreateEntity(input, createdBy);
        _db.UserAccounts.Add(account);
        await _db.SaveChangesAsync();
        return _manager.MapToOutput(account);
    }

    public async Task<OutputGetAccount?> UpdateAsync(long id, InputUpdateAccount input, string updatedBy)
    {
        var existing = await _manager.LoadActiveByIdAsync(id);
        if (existing is null)
        {
            return null;
        }

        _manager.ApplyUpdate(existing, input, updatedBy);
        await _db.SaveChangesAsync();
        return _manager.MapToOutput(existing);
    }

    public Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null)
    {
        var query = _db.UserAccounts
            .Where(a => a.Username == username && !a.IsCancelled);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.ID_UserAccounts != excludeId.Value);
        }

        return query.AnyAsync();
    }
}
