using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Entities;
using MidnightApi.Repositories.Managers;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class FamiliesRepository : IFamiliesRepository
{
    private readonly DbConnectionClass _db;
    private readonly FamiliesRepositoryManager _manager;

    public FamiliesRepository(DbConnectionClass db, FamiliesRepositoryManager manager)
    {
        _db = db;
        _manager = manager;
    }

    public async Task<OutputGetFamily?> GetByIdAsync(long id)
    {
        var family = await _manager.LoadActiveByIdAsync(id);
        return family is null ? null : _manager.MapToOutput(family);
    }

    public async Task<OutputGetFamily> CreateAsync(InputCreateFamily input, string createdBy)
    {
        var family = _manager.BuildCreateEntity(input, createdBy);
        _db.Families.Add(family);
        await _db.SaveChangesAsync();
        return _manager.MapToOutput(family);
    }

    public async Task<OutputGetFamily?> UpdateAsync(long id, InputUpdateFamily input, string updatedBy)
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

    public Task<bool> ExistsByCodeAsync(string code, long? excludeFamilyId = null)
    {
        var query = _db.Families
            .Where(f => f.FamilyCode == code && !f.IsCancelled);

        if (excludeFamilyId.HasValue)
        {
            query = query.Where(f => f.ID_Families != excludeFamilyId.Value);
        }

        return query.AnyAsync();
    }
}
