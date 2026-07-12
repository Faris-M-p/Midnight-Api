using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories;

using MidnightApi.Models.Api;

public class MembersRepository : IMembersRepository
{
    private readonly DbConnectionClass _db;

    public MembersRepository(DbConnectionClass db)
    {
        _db = db;
    }

    public async Task<List<OutputGetMember>> GetAllAsync()
    {
        var members = await _db.Members
            .Where(member => !member.IsCancelled)
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToListAsync();

        return members.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMember?> GetByIdAsync(long id)
    {
        var member = await _db.Members
            .FirstOrDefaultAsync(member => member.ID_Members == id && !member.IsCancelled);

        return member is null ? null : MapToOutput(member);
    }

    public async Task<OutputGetMemberProfile?> GetByIdWithDetailsAsync(long id)
    {
        var member = await _db.Members
            .Where(member => member.ID_Members == id && !member.IsCancelled)
            .Include(member => member.Addresses.Where(address => !address.IsCancelled))
            .Include(member => member.Images.Where(image => !image.IsCancelled))
            .Include(member => member.Events.Where(memberEvent => !memberEvent.IsCancelled))
            .Include(member => member.SocialLinks.Where(link => !link.IsCancelled))
            .Include(member => member.Notes.Where(note => !note.IsCancelled))
            .FirstOrDefaultAsync();

        return member is null ? null : MapToProfileOutput(member);
    }

    public async Task<List<OutputGetMember>> GetByFamilyIdAsync(InputGetFamilyMembers input)
    {
        var members = await _db.Members
            .Where(member => member.FK_Families == input.FamilyId && !member.IsCancelled)
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToListAsync();

        return members.Select(MapToOutput).ToList();
    }

    public async Task<OutputGetMember?> GetRootByFamilyIdAsync(long familyId)
    {
        var member = await _db.Members
            .FirstOrDefaultAsync(member =>
                member.FK_Families == familyId && member.IsRoot && !member.IsCancelled);

        return member is null ? null : MapToOutput(member);
    }

    public async Task<OutputGetMemberTree?> GetRootWithTreeDataAsync(InputGetMemberTree input)
    {
        var root = await _db.Members
            .Where(member => member.FK_Families == input.FamilyId && member.IsRoot && !member.IsCancelled)
            .Include(member => member.Images.Where(image => !image.IsCancelled))
            .Include(member => member.Events.Where(memberEvent => !memberEvent.IsCancelled))
            .Include(member => member.Notes.Where(note => !note.IsCancelled))
            .Include(member => member.SocialLinks.Where(link => !link.IsCancelled))
            .FirstOrDefaultAsync();

        if (root is null)
        {
            return null;
        }

        await LoadSpouseDetailsAsync(root);
        await LoadChildrenRecursiveAsync(root);
        return MapToTreeOutput(root);
    }

    public async Task<List<OutputGetMember>> SearchByNameAsync(InputSearchMembers input)
    {
        var normalized = input.Name.Trim().ToLower();
        var query = _db.Members.Where(member => !member.IsCancelled);

        if (input.FamilyId.HasValue)
        {
            query = query.Where(member => member.FK_Families == input.FamilyId.Value);
        }

        var members = await query
            .Where(member =>
                (member.FirstName + " " + member.LastName).ToLower().Contains(normalized) ||
                member.FirstName.ToLower().Contains(normalized) ||
                member.LastName.ToLower().Contains(normalized))
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToListAsync();

        return members.Select(MapToOutput).ToList();
    }

    public async Task<List<OutputGetMember>> GetByGenerationAsync(InputGetMembersByGeneration input)
    {
        if (input.Level < 0)
        {
            return [];
        }

        var roots = await _db.Members
            .Where(member => member.FK_Families == input.FamilyId && member.IsRoot && !member.IsCancelled)
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToListAsync();

        if (input.Level == 0)
        {
            return roots.Select(MapToOutput).ToList();
        }

        var currentGenerationIds = roots.Select(member => member.ID_Members).ToList();

        for (var generation = 1; generation <= input.Level; generation++)
        {
            var nextGeneration = await _db.Members
                .Where(member =>
                    member.FK_Families == input.FamilyId &&
                    !member.IsCancelled &&
                    member.FK_Members_Parent.HasValue &&
                    currentGenerationIds.Contains(member.FK_Members_Parent.Value))
                .OrderBy(member => member.FirstName)
                .ThenBy(member => member.LastName)
                .ToListAsync();

            if (generation == input.Level)
            {
                return nextGeneration.Select(MapToOutput).ToList();
            }

            currentGenerationIds = nextGeneration.Select(member => member.ID_Members).ToList();
        }

        return [];
    }

    public async Task<OutputGetMember> CreateAsync(InputCreateMember input, string createdBy)
    {
        var member = MapToEntity(input, createdBy);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();
        return MapToOutput(member);
    }

    public async Task<OutputGetMember?> UpdateAsync(long id, InputUpdateMember input, string updatedBy)
    {
        var existing = await _db.Members
            .FirstOrDefaultAsync(member => member.ID_Members == id && !member.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        existing.FK_Members_Parent = input.FK_Members_Parent;
        existing.FK_Members_Spouse = input.FK_Members_Spouse;
        existing.FirstName = input.FirstName;
        existing.LastName = input.LastName;
        existing.Email = input.Email;
        existing.Phone = input.Phone;
        existing.Gender = input.Gender;
        existing.DateOfBirth = input.DateOfBirth;
        existing.DateOfDeath = input.DateOfDeath;
        existing.IsRoot = input.IsRoot;
        existing.Biography = input.Biography;
        existing.Profession = input.Profession;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToOutput(existing);
    }

    public async Task<bool> SoftDeleteAsync(long id, string deletedBy)
    {
        var existing = await _db.Members
            .FirstOrDefaultAsync(member => member.ID_Members == id && !member.IsCancelled);
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

    public async Task<bool> HasRootMemberAsync(long familyId, long? excludeMemberId = null)
    {
        var query = _db.Members
            .Where(member => member.FK_Families == familyId && member.IsRoot && !member.IsCancelled);

        if (excludeMemberId.HasValue)
        {
            query = query.Where(member => member.ID_Members != excludeMemberId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<long?> GetParentIdAsync(long memberId)
    {
        return await _db.Members
            .Where(member => member.ID_Members == memberId && !member.IsCancelled)
            .Select(member => member.FK_Members_Parent)
            .FirstOrDefaultAsync();
    }

    public async Task LinkSpouseAsync(long memberId, long spouseId, string updatedBy)
    {
        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.ID_Members == memberId && !m.IsCancelled);
        var spouse = await _db.Members
            .FirstOrDefaultAsync(m => m.ID_Members == spouseId && !m.IsCancelled);

        if (member is null || spouse is null)
        {
            return;
        }

        member.FK_Members_Spouse = spouseId;
        member.UpdatedBy = updatedBy;
        member.UpdatedOn = DateTime.UtcNow;

        spouse.FK_Members_Spouse = memberId;
        spouse.UpdatedBy = updatedBy;
        spouse.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private async Task LoadSpouseDetailsAsync(Member member)
    {
        if (!member.FK_Members_Spouse.HasValue)
        {
            return;
        }

        await _db.Entry(member)
            .Reference(m => m.Spouse)
            .Query()
            .Where(spouse => !spouse.IsCancelled)
            .Include(spouse => spouse.Images.Where(image => !image.IsCancelled))
            .Include(spouse => spouse.Events.Where(memberEvent => !memberEvent.IsCancelled))
            .Include(spouse => spouse.Notes.Where(note => !note.IsCancelled))
            .Include(spouse => spouse.SocialLinks.Where(link => !link.IsCancelled))
            .LoadAsync();
    }

    private async Task LoadChildrenRecursiveAsync(Member member)
    {
        await _db.Entry(member)
            .Collection(m => m.Children)
            .Query()
            .Where(child => !child.IsCancelled)
            .Include(child => child.Images.Where(image => !image.IsCancelled))
            .Include(child => child.Events.Where(memberEvent => !memberEvent.IsCancelled))
            .Include(child => child.Notes.Where(note => !note.IsCancelled))
            .Include(child => child.SocialLinks.Where(link => !link.IsCancelled))
            .OrderBy(child => child.DateOfBirth)
            .ThenBy(child => child.FirstName)
            .LoadAsync();

        foreach (var child in member.Children.Where(child => !child.IsCancelled))
        {
            await LoadSpouseDetailsAsync(child);
            await LoadChildrenRecursiveAsync(child);
        }
    }

    private static Member MapToEntity(InputCreateMember input, string createdBy) => new()
    {
        FK_Families = input.FK_Families,
        FK_Members_Parent = input.FK_Members_Parent,
        FK_Members_Spouse = input.FK_Members_Spouse,
        FirstName = input.FirstName,
        LastName = input.LastName,
        Email = input.Email,
        Phone = input.Phone,
        Gender = input.Gender,
        DateOfBirth = input.DateOfBirth,
        DateOfDeath = input.DateOfDeath,
        IsRoot = input.IsRoot,
        Biography = input.Biography,
        Profession = input.Profession,
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow
    };

    private static OutputGetMember MapToOutput(Member member) => new()
    {
        ID_Members = member.ID_Members,
        FK_Families = member.FK_Families,
        FK_Members_Parent = member.FK_Members_Parent,
        FK_Members_Spouse = member.FK_Members_Spouse,
        FirstName = member.FirstName,
        LastName = member.LastName,
        FullName = $"{member.FirstName} {member.LastName}".Trim(),
        Email = member.Email,
        Phone = member.Phone,
        Gender = member.Gender,
        DateOfBirth = member.DateOfBirth,
        DateOfDeath = member.DateOfDeath,
        IsRoot = member.IsRoot,
        Biography = member.Biography,
        Profession = member.Profession
    };

    private static OutputGetMemberProfile MapToProfileOutput(Member member)
    {
        var output = new OutputGetMemberProfile();
        CopyMemberFields(member, output);
        output.Addresses = member.Addresses.Where(address => !address.IsCancelled).Select(MapToAddressOutput).ToList();
        output.Images = member.Images.Where(image => !image.IsCancelled).Select(MapToImageOutput).ToList();
        output.Events = member.Events.Where(memberEvent => !memberEvent.IsCancelled).Select(MapToEventOutput).ToList();
        output.SocialLinks = member.SocialLinks.Where(link => !link.IsCancelled).Select(MapToSocialLinkOutput).ToList();
        output.Notes = member.Notes.Where(note => !note.IsCancelled).Select(MapToNoteOutput).ToList();
        return output;
    }

    private static OutputGetMemberTree MapToTreeOutput(Member member, bool includeSpouse = true, bool includeChildren = true)
    {
        var output = new OutputGetMemberTree
        {
            ID_Members = member.ID_Members,
            FK_Families = member.FK_Families,
            FirstName = member.FirstName,
            LastName = member.LastName,
            FullName = $"{member.FirstName} {member.LastName}".Trim(),
            Email = member.Email,
            Phone = member.Phone,
            Gender = member.Gender,
            DateOfBirth = member.DateOfBirth,
            DateOfDeath = member.DateOfDeath,
            IsRoot = member.IsRoot,
            Biography = member.Biography,
            Profession = member.Profession,
            Images = member.Images.Where(image => !image.IsCancelled).Select(MapToImageOutput).ToList(),
            SocialLinks = member.SocialLinks.Where(link => !link.IsCancelled).Select(MapToSocialLinkOutput).ToList(),
            Events = member.Events.Where(memberEvent => !memberEvent.IsCancelled).Select(MapToEventOutput).ToList(),
            Notes = member.Notes.Where(note => !note.IsCancelled).Select(MapToNoteOutput).ToList()
        };

        if (includeSpouse && member.Spouse is not null && !member.Spouse.IsCancelled)
        {
            output.Spouse = MapToTreeOutput(member.Spouse, includeSpouse: false, includeChildren: false);
        }

        if (includeChildren)
        {
            output.Children = member.Children
                .Where(child => !child.IsCancelled)
                .OrderBy(child => child.DateOfBirth)
                .ThenBy(child => child.FirstName)
                .Select(child => MapToTreeOutput(child))
                .ToList();
        }

        return output;
    }

    private static void CopyMemberFields(Member member, OutputGetMember output)
    {
        output.ID_Members = member.ID_Members;
        output.FK_Families = member.FK_Families;
        output.FK_Members_Parent = member.FK_Members_Parent;
        output.FK_Members_Spouse = member.FK_Members_Spouse;
        output.FirstName = member.FirstName;
        output.LastName = member.LastName;
        output.FullName = $"{member.FirstName} {member.LastName}".Trim();
        output.Email = member.Email;
        output.Phone = member.Phone;
        output.Gender = member.Gender;
        output.DateOfBirth = member.DateOfBirth;
        output.DateOfDeath = member.DateOfDeath;
        output.IsRoot = member.IsRoot;
        output.Biography = member.Biography;
        output.Profession = member.Profession;
    }

    private static OutputGetMemberAddress MapToAddressOutput(MemberAddress address) => new()
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

    private static OutputGetMemberImage MapToImageOutput(MemberImage image) => new()
    {
        ID_MemberImages = image.ID_MemberImages,
        FK_Members = image.FK_Members,
        ImageUrl = image.ImageUrl,
        Caption = image.Caption,
        IsPrimary = image.IsPrimary,
        SortOrder = image.SortOrder
    };

    private static OutputGetMemberEvent MapToEventOutput(MemberEvent memberEvent) => new()
    {
        ID_MemberEvents = memberEvent.ID_MemberEvents,
        FK_Members = memberEvent.FK_Members,
        EventType = memberEvent.EventType,
        Title = memberEvent.Title,
        Description = memberEvent.Description,
        EventDate = memberEvent.EventDate
    };

    private static OutputGetMemberNote MapToNoteOutput(MemberNote note) => new()
    {
        ID_MemberNotes = note.ID_MemberNotes,
        FK_Members = note.FK_Members,
        Title = note.Title,
        Content = note.Content
    };

    private static OutputGetMemberSocialLink MapToSocialLinkOutput(MemberSocialLink link) => new()
    {
        ID_MemberSocialLinks = link.ID_MemberSocialLinks,
        FK_Members = link.FK_Members,
        Platform = link.Platform,
        Url = link.Url,
        Username = link.Username
    };
}
