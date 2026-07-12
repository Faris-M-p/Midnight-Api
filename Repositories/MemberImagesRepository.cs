using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class MemberImagesRepository : IMemberImagesRepository
{
    private readonly DbConnectionClass _db;

    public MemberImagesRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<OutputGetMemberImage>> GetAllAsync()
    {
        var items = await _db.MemberImages
            .Where(image => !image.IsCancelled)
            .OrderBy(image => image.SortOrder)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberImage?> GetByIdAsync(long id)
    {
        var item = await _db.MemberImages
            .FirstOrDefaultAsync(image => image.ID_MemberImages == id && !image.IsCancelled);

        return item is null ? null : MapToOutput(item);
    }

    public async Task<List<OutputGetMemberImage>> GetByMemberIdAsync(long memberId)
    {
        var items = await _db.MemberImages
            .Where(image => image.FK_Members == memberId && !image.IsCancelled)
            .OrderBy(image => image.SortOrder)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberImage> CreateAsync(InputCreateMemberImage input, string createdBy)
    {
        var image = MapToEntity(input, createdBy);
        _db.MemberImages.Add(image);
        await _db.SaveChangesAsync();
        return MapToOutput(image);
    }

    public async Task<OutputGetMemberImage?> UpdateAsync(long id, InputUpdateMemberImage input, string updatedBy)
    {
        var existing = await _db.MemberImages
            .FirstOrDefaultAsync(image => image.ID_MemberImages == id && !image.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        existing.ImageUrl = input.ImageUrl;
        existing.Caption = input.Caption;
        existing.IsPrimary = input.IsPrimary;
        existing.SortOrder = input.SortOrder;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToOutput(existing);
    }

    public async Task<bool> SoftDeleteAsync(long id, string deletedBy)
    {
        var existing = await _db.MemberImages
            .FirstOrDefaultAsync(image => image.ID_MemberImages == id && !image.IsCancelled);
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

    private static MemberImage MapToEntity(InputCreateMemberImage input, string createdBy) => new()
    {
        FK_Members = input.FK_Members,
        ImageUrl = input.ImageUrl,
        Caption = input.Caption,
        IsPrimary = input.IsPrimary,
        SortOrder = input.SortOrder,
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow
    };

    private static OutputGetMemberImage MapToOutput(MemberImage image) => new()
    {
        ID_MemberImages = image.ID_MemberImages,
        FK_Members = image.FK_Members,
        ImageUrl = image.ImageUrl,
        Caption = image.Caption,
        IsPrimary = image.IsPrimary,
        SortOrder = image.SortOrder
    };
}
