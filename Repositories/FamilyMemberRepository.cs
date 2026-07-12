using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Repositories;

public class FamilyMemberRepository : IFamilyMemberRepository
{
    private readonly DbConnectionClass _db;

    public FamilyMemberRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<FamilyMember>> GetAllAsync()
    {
        return await _db.FamilyMembers
            .OrderBy(member => member.Name)
            .ToListAsync();
    }

    public async Task<FamilyMember?> GetByIdAsync(string id)
    {
        return await _db.FamilyMembers.FirstOrDefaultAsync(member => member.Id == id);
    }

    public async Task<FamilyMember> CreateAsync(FamilyMember member)
    {
        _db.FamilyMembers.Add(member);
        await _db.SaveChangesAsync();
        return member;
    }

    public async Task<FamilyMember?> UpdateAsync(string id, FamilyMember member)
    {
        var existing = await _db.FamilyMembers.FirstOrDefaultAsync(m => m.Id == id);
        if (existing is null)
        {
            return null;
        }

        existing.Name = member.Name;
        existing.Relation = member.Relation;
        existing.Gender = member.Gender;
        existing.Dob = member.Dob;
        existing.Location = member.Location;
        existing.Profession = member.Profession;
        existing.Avatar = member.Avatar;
        existing.Bio = member.Bio;
        existing.Education = member.Education;
        existing.Career = member.Career;
        existing.Photos = member.Photos ?? [];
        existing.IsDeceased = member.IsDeceased;
        existing.Socials = member.Socials;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _db.FamilyMembers.FirstOrDefaultAsync(member => member.Id == id);
        if (existing is null)
        {
            return false;
        }

        var spouseUnions = await _db.MarriageUnions
            .Where(union => union.Spouse1Id == id || union.Spouse2Id == id)
            .ToListAsync();
        _db.MarriageUnions.RemoveRange(spouseUnions);

        var childUnions = await _db.MarriageUnions
            .Where(union => union.ChildrenIds.Contains(id))
            .ToListAsync();
        foreach (var union in childUnions)
        {
            union.ChildrenIds = union.ChildrenIds.Where(childId => childId != id).ToList();
        }

        var milestones = await _db.Milestones
            .Where(milestone => milestone.MemberId == id)
            .ToListAsync();
        _db.Milestones.RemoveRange(milestones);

        _db.FamilyMembers.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
