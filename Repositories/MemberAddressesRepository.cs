using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class MemberAddressesRepository : IMemberAddressesRepository
{
    private readonly DbConnectionClass _db;

    public MemberAddressesRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<OutputGetMemberAddress>> GetAllAsync()
    {
        var items = await _db.MemberAddresses
            .Where(address => !address.IsCancelled)
            .OrderBy(address => address.City)
            .ThenBy(address => address.AddressLine1)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberAddress?> GetByIdAsync(long id)
    {
        var item = await _db.MemberAddresses
            .FirstOrDefaultAsync(address => address.ID_MemberAddresses == id && !address.IsCancelled);

        return item is null ? null : MapToOutput(item);
    }

    public async Task<List<OutputGetMemberAddress>> GetByMemberIdAsync(long memberId)
    {
        var items = await _db.MemberAddresses
            .Where(address => address.FK_Members == memberId && !address.IsCancelled)
            .OrderByDescending(address => address.IsPrimary)
            .ThenBy(address => address.City)
            .ToListAsync();

        return items.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMemberAddress> CreateAsync(InputCreateMemberAddress input, string createdBy)
    {
        var address = MapToEntity(input, createdBy);
        _db.MemberAddresses.Add(address);
        await _db.SaveChangesAsync();
        return MapToOutput(address);
    }

    public async Task<OutputGetMemberAddress?> UpdateAsync(long id, InputUpdateMemberAddress input, string updatedBy)
    {
        var existing = await _db.MemberAddresses
            .FirstOrDefaultAsync(address => address.ID_MemberAddresses == id && !address.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        existing.AddressLine1 = input.AddressLine1;
        existing.AddressLine2 = input.AddressLine2;
        existing.City = input.City;
        existing.State = input.State;
        existing.Country = input.Country;
        existing.PostalCode = input.PostalCode;
        existing.IsPrimary = input.IsPrimary;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToOutput(existing);
    }

    public async Task<bool> SoftDeleteAsync(long id, string deletedBy)
    {
        var existing = await _db.MemberAddresses
            .FirstOrDefaultAsync(address => address.ID_MemberAddresses == id && !address.IsCancelled);
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

    private static MemberAddress MapToEntity(InputCreateMemberAddress input, string createdBy) => new()
    {
        FK_Members = input.FK_Members,
        AddressLine1 = input.AddressLine1,
        AddressLine2 = input.AddressLine2,
        City = input.City,
        State = input.State,
        Country = input.Country,
        PostalCode = input.PostalCode,
        IsPrimary = input.IsPrimary,
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow
    };

    private static OutputGetMemberAddress MapToOutput(MemberAddress address) => new()
    {
        ID_MemberAddresses = address.ID_MemberAddresses,
        FK_Members = address.FK_Members,
        AddressLine1 = address.AddressLine1,
        AddressLine2 = address.AddressLine2,
        City = address.City,
        State = address.State,
        Country = address.Country,
        PostalCode = address.PostalCode,
        IsPrimary = address.IsPrimary
    };
}
