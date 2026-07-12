using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Repositories;

public class MilestoneRepository : IMilestoneRepository
{
    private readonly DbConnectionClass _db;

    public MilestoneRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<Milestone>> GetAllAsync()
    {
        return await _db.Milestones
            .OrderBy(milestone => milestone.Year)
            .ThenBy(milestone => milestone.Title)
            .ToListAsync();
    }

    public async Task<Milestone?> GetByIdAsync(string id)
    {
        return await _db.Milestones.FirstOrDefaultAsync(milestone => milestone.Id == id);
    }

    public async Task<Milestone> CreateAsync(Milestone milestone)
    {
        _db.Milestones.Add(milestone);
        await _db.SaveChangesAsync();
        return milestone;
    }

    public async Task<Milestone?> UpdateAsync(string id, Milestone milestone)
    {
        var existing = await _db.Milestones.FirstOrDefaultAsync(m => m.Id == id);
        if (existing is null)
        {
            return null;
        }

        existing.Year = milestone.Year;
        existing.Title = milestone.Title;
        existing.Description = milestone.Description;
        existing.MemberId = milestone.MemberId;
        existing.MemberName = milestone.MemberName;
        existing.Category = milestone.Category;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _db.Milestones.FirstOrDefaultAsync(milestone => milestone.Id == id);
        if (existing is null)
        {
            return false;
        }

        _db.Milestones.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
