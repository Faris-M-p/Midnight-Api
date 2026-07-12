using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class MemberSocialLinksRepository : IMemberSocialLinksRepository
{
    private readonly DbConnectionClass _db;

    public MemberSocialLinksRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<OutputGetMemberSocialLink>> GetAllAsync()
    {
        var items = await _db.MemberSocialLinks
            .Where(link => !link.IsCancelled)
            .OrderBy(link => link.Platform)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberSocialLink?> GetByIdAsync(long id)
    {
        var item = await _db.MemberSocialLinks
            .FirstOrDefaultAsync(link => link.ID_MemberSocialLinks == id && !link.IsCancelled);

        return item is null ? null : MapToOutput(item);
    }

    public async Task<List<OutputGetMemberSocialLink>> GetByMemberIdAsync(long memberId)
    {
        var items = await _db.MemberSocialLinks
            .Where(link => link.FK_Members == memberId && !link.IsCancelled)
            .OrderBy(link => link.Platform)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberSocialLink> CreateAsync(InputCreateMemberSocialLink input, string createdBy)
    {
        var socialLink = MapToEntity(input, createdBy);
        _db.MemberSocialLinks.Add(socialLink);
        await _db.SaveChangesAsync();
        return MapToOutput(socialLink);
    }

    public async Task<OutputGetMemberSocialLink?> UpdateAsync(long id, InputUpdateMemberSocialLink input, string updatedBy)
    {
        var existing = await _db.MemberSocialLinks
            .FirstOrDefaultAsync(link => link.ID_MemberSocialLinks == id && !link.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        existing.Platform = input.Platform;
        existing.Url = input.Url;
        existing.Username = input.Username;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToOutput(existing);
    }

    public async Task<bool> SoftDeleteAsync(long id, string deletedBy)
    {
        var existing = await _db.MemberSocialLinks
            .FirstOrDefaultAsync(link => link.ID_MemberSocialLinks == id && !link.IsCancelled);
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

    private static MemberSocialLink MapToEntity(InputCreateMemberSocialLink input, string createdBy) => new()
    {
        FK_Members = input.FK_Members,
        Platform = input.Platform,
        Url = input.Url,
        Username = input.Username,
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow
    };

    private static OutputGetMemberSocialLink MapToOutput(MemberSocialLink socialLink) => new()
    {
        ID_MemberSocialLinks = socialLink.ID_MemberSocialLinks,
        FK_Members = socialLink.FK_Members,
        Platform = socialLink.Platform,
        Url = socialLink.Url,
        Username = socialLink.Username
    };
}
