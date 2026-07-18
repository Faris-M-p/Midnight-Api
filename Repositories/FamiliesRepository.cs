using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class FamiliesRepository : IFamiliesRepository
{
    private readonly DbConnectionClass _db;

    public FamiliesRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<OutputGetFamily?> GetByIdAsync(long id)
    {
        var family = await _db.Families
            .FirstOrDefaultAsync(family => family.ID_Families == id && !family.IsCancelled);

        return family is null ? null : MapToOutput(family);
    }

    public async Task<OutputGetFamily> CreateAsync(InputCreateFamily input, string createdBy)
    {
        var family = new Family
        {
            FamilyCode = input.FamilyCode,
            FamilyName = input.FamilyName,
            Description = input.Description,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

        _db.Families.Add(family);
        await _db.SaveChangesAsync();
        return MapToOutput(family);
    }

    public async Task<OutputGetFamily?> UpdateAsync(long id, InputUpdateFamily input, string updatedBy)
    {
        var existing = await _db.Families
            .FirstOrDefaultAsync(family => family.ID_Families == id && !family.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        existing.FamilyName = input.FamilyName;
        existing.Description = input.Description;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToOutput(existing);
    }

    public async Task<bool> ExistsByCodeAsync(string code, long? excludeFamilyId = null)
    {
        var query = _db.Families
            .Where(family => family.FamilyCode == code && !family.IsCancelled);

        if (excludeFamilyId.HasValue)
        {
            query = query.Where(family => family.ID_Families != excludeFamilyId.Value);
        }

        return await query.AnyAsync();
    }

    private static OutputGetFamily MapToOutput(Family family) => new()
    {
        ID_Families = family.ID_Families,
        FamilyCode = family.FamilyCode,
        FamilyName = family.FamilyName,
        Description = family.Description
    };
}
