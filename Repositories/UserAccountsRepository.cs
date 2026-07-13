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

    public async Task<OutputGetAccount?> GetByIdAsync(long id)
    {
        var account = await _db.UserAccounts
            .FirstOrDefaultAsync(a => a.ID_UserAccounts == id && !a.IsCancelled);

        return account is null ? null : MapToOutput(account);
    }

    public async Task<OutputGetAccount?> GetByFamilyIdAsync(long familyId)
    {
        var account = await _db.UserAccounts
            .FirstOrDefaultAsync(a => a.FK_Families == familyId && !a.IsCancelled);

        return account is null ? null : MapToOutput(account);
    }

    public async Task<(OutputGetAccount Account, string PasswordHash)?> GetLoginByUsernameAsync(string username)
    {
        var account = await _db.UserAccounts
            .FirstOrDefaultAsync(a => a.Username == username && !a.IsCancelled);

        return account is null ? null : (MapToOutput(account), account.PasswordHash);
    }

    public async Task<OutputGetAccount> CreateAsync(InputCreateAccount input, string createdBy)
    {
        var account = new UserAccount
        {
            FK_Families = input.FK_Families,
            Username = input.Username,
            Email = input.Email,
            PasswordHash = input.PasswordHash,
            IsActive = input.IsActive,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

        _db.UserAccounts.Add(account);
        await _db.SaveChangesAsync();
        return MapToOutput(account);
    }

    public async Task<OutputGetAccount?> UpdateAsync(long id, InputUpdateAccount input, string updatedBy)
    {
        var existing = await _db.UserAccounts
            .FirstOrDefaultAsync(a => a.ID_UserAccounts == id && !a.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        existing.Username = input.Username;
        existing.Email = input.Email;
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
            .FirstOrDefaultAsync(a => a.ID_UserAccounts == id && !a.IsCancelled);
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
            .Where(a => a.Username == username && !a.IsCancelled);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.ID_UserAccounts != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> ExistsByFamilyIdAsync(long familyId)
    {
        return await _db.UserAccounts
            .AnyAsync(a => a.FK_Families == familyId && !a.IsCancelled);
    }

    private static OutputGetAccount MapToOutput(UserAccount account) => new()
    {
        ID_UserAccounts = account.ID_UserAccounts,
        FK_Families = account.FK_Families,
        Username = account.Username,
        Email = account.Email,
        IsActive = account.IsActive
    };
}
