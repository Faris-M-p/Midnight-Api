using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Repositories;

public class FamilyTreeRepository : IFamilyTreeRepository
{
    private readonly DbConnectionClass _db;

    public FamilyTreeRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<FamilyTreeSnapshot> GetTreeSnapshotAsync()
    {
        var members = await _db.FamilyMembers
            .OrderBy(member => member.Name)
            .ToListAsync();
        var unions = await _db.MarriageUnions
            .OrderBy(union => union.Id)
            .ToListAsync();
        var milestones = await _db.Milestones
            .OrderBy(milestone => milestone.Year)
            .ThenBy(milestone => milestone.Title)
            .ToListAsync();

        return new FamilyTreeSnapshot
        {
            Members = members,
            Unions = unions,
            Milestones = milestones
        };
    }
}
