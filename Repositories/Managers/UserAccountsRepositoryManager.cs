using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Models.Api;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories.Managers;

public class UserAccountsRepositoryManager
{
    private readonly DbConnectionClass _db;

    public UserAccountsRepositoryManager(DbConnectionClass db)
    {
        _db = db;
    }

    public Task<UserAccount?> LoadActiveByIdAsync(long id)
    {
        return _db.UserAccounts
            .FirstOrDefaultAsync(a => a.ID_UserAccounts == id && !a.IsCancelled);
    }

    public Task<UserAccount?> LoadActiveByUsernameAsync(string username)
    {
        return _db.UserAccounts
            .FirstOrDefaultAsync(a => a.Username == username && !a.IsCancelled);
    }

    public UserAccount BuildCreateEntity(InputCreateAccount input, string createdBy)
    {
        return new UserAccount
        {
            FK_Families = input.FK_Families,
            Username = input.Username,
            Email = input.Email,
            PasswordHash = input.PasswordHash,
            IsActive = input.IsActive,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };
    }

    public void ApplyUpdate(UserAccount existing, InputUpdateAccount input, string updatedBy)
    {
        existing.Username = input.Username;
        existing.Email = input.Email;
        if (!string.IsNullOrWhiteSpace(input.PasswordHash))
        {
            existing.PasswordHash = input.PasswordHash;
        }

        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;
    }

    public OutputGetAccount MapToOutput(UserAccount account) => new()
    {
        ID_UserAccounts = account.ID_UserAccounts,
        FK_Families = account.FK_Families,
        Username = account.Username,
        Email = account.Email,
        IsActive = account.IsActive
    };
}
