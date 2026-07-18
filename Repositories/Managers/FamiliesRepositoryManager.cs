using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Models.Api;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories.Managers;

public class FamiliesRepositoryManager
{
    private readonly DbConnectionClass _db;

    public FamiliesRepositoryManager(DbConnectionClass db)
    {
        _db = db;
    }

    public Task<Family?> LoadActiveByIdAsync(long id)
    {
        return _db.Families
            .FirstOrDefaultAsync(f => f.ID_Families == id && !f.IsCancelled);
    }

    public Family BuildCreateEntity(InputCreateFamily input, string createdBy)
    {
        return new Family
        {
            FamilyCode = input.FamilyCode,
            FamilyName = input.FamilyName,
            Description = input.Description,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };
    }

    public void ApplyUpdate(Family existing, InputUpdateFamily input, string updatedBy)
    {
        existing.FamilyName = input.FamilyName;
        existing.Description = input.Description;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;
    }

    public OutputGetFamily MapToOutput(Family family) => new()
    {
        ID_Families = family.ID_Families,
        FamilyCode = family.FamilyCode,
        FamilyName = family.FamilyName,
        Description = family.Description
    };
}
