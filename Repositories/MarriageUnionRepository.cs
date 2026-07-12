using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Repositories;

public class MarriageUnionRepository : IMarriageUnionRepository
{
    private readonly DbConnectionClass _db;

    public MarriageUnionRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<MarriageUnion>> GetAllAsync()
    {
        return await _db.MarriageUnions
            .OrderBy(union => union.Id)
            .ToListAsync();
    }

    public async Task<MarriageUnion?> GetByIdAsync(string id)
    {
        return await _db.MarriageUnions.FirstOrDefaultAsync(union => union.Id == id);
    }

    public async Task<MarriageUnion> CreateAsync(MarriageUnion union)
    {
        _db.MarriageUnions.Add(union);
        await _db.SaveChangesAsync();
        return union;
    }

    public async Task<MarriageUnion?> UpdateAsync(string id, MarriageUnion union)
    {
        var existing = await _db.MarriageUnions.FirstOrDefaultAsync(u => u.Id == id);
        if (existing is null)
        {
            return null;
        }

        existing.Spouse1Id = union.Spouse1Id;
        existing.Spouse2Id = union.Spouse2Id;
        existing.ChildrenIds = union.ChildrenIds ?? [];

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _db.MarriageUnions.FirstOrDefaultAsync(union => union.Id == id);
        if (existing is null)
        {
            return false;
        }

        _db.MarriageUnions.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
