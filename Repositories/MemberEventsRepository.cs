using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class MemberEventsRepository : IMemberEventsRepository
{
    private readonly DbConnectionClass _db;

    public MemberEventsRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<OutputGetMemberEvent>> GetAllAsync()
    {
        var items = await _db.MemberEvents
            .Where(memberEvent => !memberEvent.IsCancelled)
            .OrderByDescending(memberEvent => memberEvent.EventDate)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberEvent?> GetByIdAsync(long id)
    {
        var item = await _db.MemberEvents
            .FirstOrDefaultAsync(memberEvent => memberEvent.ID_MemberEvents == id && !memberEvent.IsCancelled);

        return item is null ? null : MapToOutput(item);
    }

    public async Task<List<OutputGetMemberEvent>> GetByMemberIdAsync(long memberId)
    {
        var items = await _db.MemberEvents
            .Where(memberEvent => memberEvent.FK_Members == memberId && !memberEvent.IsCancelled)
            .OrderByDescending(memberEvent => memberEvent.EventDate)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberEvent> CreateAsync(InputCreateMemberEvent input, string createdBy)
    {
        var memberEvent = MapToEntity(input, createdBy);
        _db.MemberEvents.Add(memberEvent);
        await _db.SaveChangesAsync();
        return MapToOutput(memberEvent);
    }

    public async Task<OutputGetMemberEvent?> UpdateAsync(long id, InputUpdateMemberEvent input, string updatedBy)
    {
        var existing = await _db.MemberEvents
            .FirstOrDefaultAsync(memberEvent => memberEvent.ID_MemberEvents == id && !memberEvent.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        existing.EventType = input.EventType;
        existing.Title = input.Title;
        existing.Description = input.Description;
        existing.EventDate = input.EventDate;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToOutput(existing);
    }

    public async Task<bool> SoftDeleteAsync(long id, string deletedBy)
    {
        var existing = await _db.MemberEvents
            .FirstOrDefaultAsync(memberEvent => memberEvent.ID_MemberEvents == id && !memberEvent.IsCancelled);
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

    private static MemberEvent MapToEntity(InputCreateMemberEvent input, string createdBy) => new()
    {
        FK_Members = input.FK_Members,
        EventType = input.EventType,
        Title = input.Title,
        Description = input.Description,
        EventDate = input.EventDate,
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow
    };

    private static OutputGetMemberEvent MapToOutput(MemberEvent memberEvent) => new()
    {
        ID_MemberEvents = memberEvent.ID_MemberEvents,
        FK_Members = memberEvent.FK_Members,
        EventType = memberEvent.EventType,
        Title = memberEvent.Title,
        Description = memberEvent.Description,
        EventDate = memberEvent.EventDate
    };
}
