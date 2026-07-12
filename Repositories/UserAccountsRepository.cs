using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class UserAccountsRepository : IUserAccountsRepository
{
    private readonly DbConnectionClass _db;

    public UserAccountsRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<OutputGetAccount>> GetAllAsync()
    {
        var accounts = await _db.UserAccounts
            .Where(account => !account.IsCancelled)
            .OrderBy(account => account.Username)
            .ToListAsync();

        return accounts.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetAccount?> GetByIdAsync(long id)
    {
        var account = await _db.UserAccounts
            .FirstOrDefaultAsync(account => account.ID_UserAccounts == id && !account.IsCancelled);

        return account is null ? null : MapToOutput(account);
    }

    public async Task<OutputGetAccount?> GetByMemberIdAsync(long memberId)
    {
        var account = await _db.UserAccounts
            .FirstOrDefaultAsync(account => account.FK_Members == memberId && !account.IsCancelled);

        return account is null ? null : MapToOutput(account);
    }

    public async Task<OutputGetAccount> CreateAsync(InputCreateAccount input, string createdBy)
    {
        var account = MapToEntity(input, createdBy);
        _db.UserAccounts.Add(account);
        await _db.SaveChangesAsync();
        return MapToOutput(account);
    }

    public async Task<OutputGetAccount?> UpdateAsync(long id, InputUpdateAccount input, string updatedBy)
    {
        var existing = await _db.UserAccounts
            .FirstOrDefaultAsync(account => account.ID_UserAccounts == id && !account.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        existing.Username = input.Username;
        existing.Email = input.Email;
        existing.IsActive = input.IsActive;
        if (!string.IsNullOrWhiteSpace(input.PasswordHash))
        {
            existing.PasswordHash = input.PasswordHash;
        }
        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToOutput(existing);
    }

    public async Task<bool> SoftDeleteAsync(long id, string deletedBy)
    {
        var existing = await _db.UserAccounts
            .FirstOrDefaultAsync(account => account.ID_UserAccounts == id && !account.IsCancelled);
        if (existing is null)
        {
            return false;
        }

        existing.IsCancelled = true;
        existing.CancelledBy = deletedBy;
        existing.CancelledOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null)
    {
        var query = _db.UserAccounts
            .Where(account => account.Username == username && !account.IsCancelled);

        if (excludeId.HasValue)
        {
            query = query.Where(account => account.ID_UserAccounts != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    private static UserAccount MapToEntity(InputCreateAccount input, string createdBy) => new()
    {
        FK_Members = input.FK_Members,
        Username = input.Username,
        Email = input.Email,
        PasswordHash = input.PasswordHash,
        IsActive = input.IsActive,
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow
    };

    private static OutputGetAccount MapToOutput(UserAccount account) => new()
    {
        ID_UserAccounts = account.ID_UserAccounts,
        FK_Members = account.FK_Members,
        Username = account.Username,
        Email = account.Email,
        IsActive = account.IsActive
    };
}
