using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class MemberNotesRepository : IMemberNotesRepository
{
    private readonly DbConnectionClass _db;

    public MemberNotesRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<OutputGetMemberNote>> GetAllAsync()
    {
        var items = await _db.MemberNotes
            .Where(note => !note.IsCancelled)
            .OrderByDescending(note => note.CreatedOn)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberNote?> GetByIdAsync(long id)
    {
        var item = await _db.MemberNotes
            .FirstOrDefaultAsync(note => note.ID_MemberNotes == id && !note.IsCancelled);

        return item is null ? null : MapToOutput(item);
    }

    public async Task<List<OutputGetMemberNote>> GetByMemberIdAsync(long memberId)
    {
        var items = await _db.MemberNotes
            .Where(note => note.FK_Members == memberId && !note.IsCancelled)
            .OrderByDescending(note => note.CreatedOn)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberNote> CreateAsync(InputCreateMemberNote input, string createdBy)
    {
        var note = MapToEntity(input, createdBy);
        _db.MemberNotes.Add(note);
        await _db.SaveChangesAsync();
        return MapToOutput(note);
    }

    public async Task<OutputGetMemberNote?> UpdateAsync(long id, InputUpdateMemberNote input, string updatedBy)
    {
        var existing = await _db.MemberNotes
            .FirstOrDefaultAsync(note => note.ID_MemberNotes == id && !note.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        existing.Title = input.Title;
        existing.Content = input.Content;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToOutput(existing);
    }

    public async Task<bool> SoftDeleteAsync(long id, string deletedBy)
    {
        var existing = await _db.MemberNotes
            .FirstOrDefaultAsync(note => note.ID_MemberNotes == id && !note.IsCancelled);
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

    private static MemberNote MapToEntity(InputCreateMemberNote input, string createdBy) => new()
    {
        FK_Members = input.FK_Members,
        Title = input.Title,
        Content = input.Content,
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow
    };

    private static OutputGetMemberNote MapToOutput(MemberNote note) => new()
    {
        ID_MemberNotes = note.ID_MemberNotes,
        FK_Members = note.FK_Members,
        Title = note.Title,
        Content = note.Content
    };
}
