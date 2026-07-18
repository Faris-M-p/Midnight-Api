using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Models.Api;
using MidnightApi.Models.Entities;
using MidnightApi.Repositories.Managers;
using MidnightApi.Services;

namespace MidnightApi.Repositories;

public class MembersRepository : IMembersRepository
{
    private readonly DbConnectionClass _db;
    private readonly MembersRepositoryManager _manager;
    private readonly MemberValidationService _validation;

    public MembersRepository(DbConnectionClass db, MembersRepositoryManager manager, MemberValidationService validation)
    {
        _db = db;
        _manager = manager;
        _validation = validation;
    }

    public async Task<OutputPagedMembers> GetListAsync(long familyId, InputMemberListQuery query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var members = _db.Members
            .AsNoTracking()
            .Where(m => m.FK_Families == familyId && !m.IsCancelled);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            members = members.Where(m =>
                m.FirstName.ToLower().Contains(term)
                || m.LastName.ToLower().Contains(term)
                || (m.FirstName + " " + m.LastName).ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Gender))
        {
            members = members.Where(m => m.Gender == query.Gender);
        }

        members = (query.SortBy?.ToLower()) switch
        {
            "lastname" => query.SortDesc
                ? members.OrderByDescending(m => m.LastName).ThenBy(m => m.FirstName)
                : members.OrderBy(m => m.LastName).ThenBy(m => m.FirstName),
            "dob" => query.SortDesc
                ? members.OrderByDescending(m => m.DateOfBirth)
                : members.OrderBy(m => m.DateOfBirth),
            _ => query.SortDesc
                ? members.OrderByDescending(m => m.FirstName).ThenByDescending(m => m.LastName)
                : members.OrderBy(m => m.FirstName).ThenBy(m => m.LastName)
        };

        var total = await members.CountAsync();
        var rows = await members
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.Images.Where(i => !i.IsCancelled))
            .ToListAsync();

        return new OutputPagedMembers
        {
            Items = rows.Select(_manager.MapToListItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<OutputMemberProfile?> GetProfileAsync(long familyId, long memberId)
    {
        var member = await _manager.LoadProfileEntityAsync(familyId, memberId);
        return member is null ? null : await _manager.MapToProfileAsync(member);
    }

    public async Task<OutputMemberProfile> CreateAsync(long familyId, InputSaveMember input, string createdBy)
    {
        await ValidateSaveBusinessAsync(familyId, input, memberId: null);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var member = await _manager.CreateMemberAsync(familyId, input, createdBy);

            await _manager.SaveAddressesAsync(member.ID_Members, input.Addresses, createdBy);
            await _manager.SaveImagesAsync(member.ID_Members, input.Images, createdBy);
            await _manager.SaveEventsAsync(member.ID_Members, input.Events, createdBy);
            await _manager.SaveNotesAsync(member.ID_Members, input.Notes, createdBy);
            await _manager.SaveSocialLinksAsync(member.ID_Members, input.SocialLinks, createdBy);
            await _manager.LinkSpouseAsync(member, input.SpouseId, createdBy);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return BuildCreateResponse(member, input);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<OutputMemberProfile?> UpdateAsync(long familyId, long memberId, InputSaveMember input, string updatedBy)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var existing = await _db.Members
                .FirstOrDefaultAsync(m => m.ID_Members == memberId && m.FK_Families == familyId && !m.IsCancelled);
            if (existing is null)
            {
                return null;
            }

            await ValidateSaveBusinessAsync(familyId, input, memberId);

            _manager.ApplyBasicFields(existing, input, updatedBy);
            await _manager.SyncNestedAsync(memberId, input, updatedBy, replaceMissing: true);
            await _manager.ApplySpouseLinkAsync(existing, input.SpouseId, updatedBy);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return await GetProfileAsync(familyId, memberId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> SoftDeleteAsync(long familyId, long memberId, string deletedBy)
    {
        var existing = await _db.Members
            .FirstOrDefaultAsync(m => m.ID_Members == memberId && m.FK_Families == familyId && !m.IsCancelled);
        if (existing is null)
        {
            return false;
        }

        if (existing.FK_Members_Spouse.HasValue)
        {
            var spouse = await _db.Members
                .FirstOrDefaultAsync(m => m.ID_Members == existing.FK_Members_Spouse && !m.IsCancelled);
            if (spouse is not null)
            {
                spouse.FK_Members_Spouse = null;
                spouse.UpdatedBy = deletedBy;
                spouse.UpdatedOn = DateTime.UtcNow;
            }

            existing.FK_Members_Spouse = null;
        }

        existing.IsCancelled = true;
        existing.CancelledBy = deletedBy;
        existing.CancelledOn = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<OutputMemberProfile> AddChildAsync(long familyId, long parentId, InputSaveMember child, string createdBy)
    {
        var parent = await _db.Members
            .FirstOrDefaultAsync(m => m.ID_Members == parentId && m.FK_Families == familyId && !m.IsCancelled)
            ?? throw new NotFoundException("Parent member not found.");

        child.ParentId = parent.ID_Members;
        child.IsRoot = false;
        return await CreateAsync(familyId, child, createdBy);
    }

    public async Task<OutputMemberProfile> AddSpouseAsync(long familyId, long memberId, InputSaveMember spouseInput, string createdBy)
    {
        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.ID_Members == memberId && m.FK_Families == familyId && !m.IsCancelled)
            ?? throw new NotFoundException("Member not found.");

        if (member.FK_Members_Spouse.HasValue)
        {
            throw new ConflictException("Member already has a spouse.");
        }

        spouseInput.IsRoot = false;
        spouseInput.ParentId = null;
        spouseInput.SpouseId = null;
        await ValidateSaveBusinessAsync(familyId, spouseInput, memberId: null);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var spouse = _manager.MapToEntity(familyId, spouseInput, createdBy);
            _db.Members.Add(spouse);
            await _db.SaveChangesAsync();

            await _manager.SyncNestedAsync(spouse.ID_Members, spouseInput, createdBy, replaceMissing: false);

            member.FK_Members_Spouse = spouse.ID_Members;
            spouse.FK_Members_Spouse = member.ID_Members;
            member.UpdatedBy = createdBy;
            member.UpdatedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (await GetProfileAsync(familyId, spouse.ID_Members))!;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task MapSpouseAsync(long familyId, long memberId, long spouseId, string updatedBy)
    {
        if (!await ExistsInFamilyAsync(familyId, memberId) || !await ExistsInFamilyAsync(familyId, spouseId))
        {
            throw new NotFoundException("One or both members were not found in this family.");
        }

        var validationError = await _validation.ValidateMapSpouseAsync(
            memberId,
            spouseId,
            async id => await GetRelationAsync(familyId, id)
                ?? throw new NotFoundException("Member not found."));
        if (validationError is not null)
        {
            throw new BadRequestException(validationError);
        }

        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.ID_Members == memberId && m.FK_Families == familyId && !m.IsCancelled)
            ?? throw new NotFoundException("Member not found.");

        var spouse = await _db.Members
            .FirstOrDefaultAsync(m => m.ID_Members == spouseId && m.FK_Families == familyId && !m.IsCancelled)
            ?? throw new NotFoundException("Spouse member not found.");

        member.FK_Members_Spouse = spouse.ID_Members;
        spouse.FK_Members_Spouse = member.ID_Members;
        member.UpdatedBy = updatedBy;
        spouse.UpdatedBy = updatedBy;
        member.UpdatedOn = DateTime.UtcNow;
        spouse.UpdatedOn = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<OutputFamilyTree> GetTreeAsync(long familyId)
    {
        var total = await _db.Members.CountAsync(m => m.FK_Families == familyId && !m.IsCancelled);
        var root = await _db.Members
            .Where(m => m.FK_Families == familyId && m.IsRoot && !m.IsCancelled)
            .Include(m => m.Images.Where(i => !i.IsCancelled))
            .FirstOrDefaultAsync();

        if (root is null)
        {
            return new OutputFamilyTree { TotalMembers = total };
        }

        await _manager.LoadSpouseDetailsAsync(root);
        await _manager.LoadChildrenRecursiveAsync(root);

        return new OutputFamilyTree
        {
            Root = _manager.MapToTreeNode(root),
            TotalMembers = total
        };
    }

    public async Task<OutputDashboard> GetDashboardAsync(long familyId)
    {
        var family = await _db.Families
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ID_Families == familyId && !f.IsCancelled)
            ?? throw new NotFoundException("Family not found.");

        var members = await _db.Members
            .AsNoTracking()
            .Where(m => m.FK_Families == familyId && !m.IsCancelled)
            .Include(m => m.Images.Where(i => !i.IsCancelled))
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcoming = members
            .Where(m => m.DateOfBirth.HasValue && m.DateOfDeath is null)
            .Select(m =>
            {
                var dob = m.DateOfBirth!.Value;
                var next = new DateOnly(today.Year, dob.Month, Math.Min(dob.Day, DateTime.DaysInMonth(today.Year, dob.Month)));
                if (next < today)
                {
                    next = new DateOnly(today.Year + 1, dob.Month, Math.Min(dob.Day, DateTime.DaysInMonth(today.Year + 1, dob.Month)));
                }

                return new OutputUpcomingBirthday
                {
                    MemberId = m.ID_Members,
                    FullName = $"{m.FirstName} {m.LastName}",
                    DateOfBirth = dob,
                    TurningAge = next.Year - dob.Year,
                    DaysUntil = next.DayNumber - today.DayNumber
                };
            })
            .OrderBy(x => x.DaysUntil)
            .Take(5)
            .ToList();

        return new OutputDashboard
        {
            Family = new OutputGetFamily
            {
                ID_Families = family.ID_Families,
                FamilyCode = family.FamilyCode,
                FamilyName = family.FamilyName,
                Description = family.Description
            },
            TotalMembers = members.Count,
            TotalGenerations = await _manager.CountGenerationsAsync(familyId),
            RecentMembers = members
                .OrderByDescending(m => m.CreatedOn)
                .Take(5)
                .Select(_manager.MapToListItem)
                .ToList(),
            UpcomingBirthdays = upcoming,
            Stats = new OutputDashboardStats
            {
                MaleCount = members.Count(m => string.Equals(m.Gender, "Male", StringComparison.OrdinalIgnoreCase)),
                FemaleCount = members.Count(m => string.Equals(m.Gender, "Female", StringComparison.OrdinalIgnoreCase)),
                OtherGenderCount = members.Count(m =>
                    !string.Equals(m.Gender, "Male", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(m.Gender, "Female", StringComparison.OrdinalIgnoreCase)),
                LivingCount = members.Count(m => m.DateOfDeath is null),
                DeceasedCount = members.Count(m => m.DateOfDeath is not null)
            }
        };
    }

    public async Task<List<OutputTimelineItem>> GetTimelineAsync(long familyId)
    {
        var events = await _db.MemberEvents
            .AsNoTracking()
            .Where(e => !e.IsCancelled && e.Member.FK_Families == familyId && !e.Member.IsCancelled)
            .Include(e => e.Member)
            .OrderByDescending(e => e.EventDate)
            .ThenBy(e => e.Title)
            .ToListAsync();

        return events.Select(e => new OutputTimelineItem
        {
            EventId = e.ID_MemberEvents,
            MemberId = e.FK_Members,
            MemberName = $"{e.Member.FirstName} {e.Member.LastName}",
            EventType = e.EventType,
            Title = e.Title,
            Description = e.Description,
            EventDate = e.EventDate
        }).ToList();
    }

    public Task<bool> ExistsInFamilyAsync(long familyId, long memberId) =>
        _db.Members.AnyAsync(m => m.ID_Members == memberId && m.FK_Families == familyId && !m.IsCancelled);

    public Task<bool> HasRootMemberAsync(long familyId, long? excludeMemberId = null)
    {
        var query = _db.Members.Where(m => m.FK_Families == familyId && m.IsRoot && !m.IsCancelled);
        if (excludeMemberId.HasValue)
        {
            query = query.Where(m => m.ID_Members != excludeMemberId.Value);
        }

        return query.AnyAsync();
    }

    public async Task<long?> GetParentIdAsync(long memberId)
    {
        return await _db.Members
            .Where(m => m.ID_Members == memberId && !m.IsCancelled)
            .Select(m => m.FK_Members_Parent)
            .FirstOrDefaultAsync();
    }

    public async Task<(long? ParentId, long? SpouseId)?> GetRelationAsync(long familyId, long memberId)
    {
        var row = await _db.Members
            .AsNoTracking()
            .Where(m => m.ID_Members == memberId && m.FK_Families == familyId && !m.IsCancelled)
            .Select(m => new { m.FK_Members_Parent, m.FK_Members_Spouse })
            .FirstOrDefaultAsync();

        return row is null ? null : (row.FK_Members_Parent, row.FK_Members_Spouse);
    }

    private static OutputMemberProfile BuildCreateResponse(Member member, InputSaveMember input)
    {
        return new OutputMemberProfile
        {
            Id = member.ID_Members,
            FirstName = member.FirstName,
            LastName = member.LastName,
            FullName = $"{member.FirstName} {member.LastName}",
            Email = member.Email,
            Phone = member.Phone,
            Gender = member.Gender,
            DateOfBirth = member.DateOfBirth,
            DateOfDeath = member.DateOfDeath,
            IsRoot = member.IsRoot,
            Biography = member.Biography,
            Profession = member.Profession,
            Addresses = input.Addresses?.Select(a => new MemberAddressItem
            {
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                City = a.City,
                State = a.State,
                Country = a.Country,
                PostalCode = a.PostalCode,
                IsPrimary = a.IsPrimary
            }).ToList() ?? [],
            Images = input.Images?.Select(i => new MemberImageItem
            {
                ImageUrl = i.ImageUrl,
                Caption = i.Caption,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            }).ToList() ?? [],
            Events = input.Events?.Select(e => new MemberEventItem
            {
                EventType = e.EventType,
                Title = e.Title,
                Description = e.Description,
                EventDate = e.EventDate
            }).ToList() ?? [],
            Notes = input.Notes?.Select(n => new MemberNoteItem
            {
                Title = n.Title,
                Content = n.Content
            }).ToList() ?? [],
            SocialLinks = input.SocialLinks?.Select(s => new MemberSocialLinkItem
            {
                Platform = s.Platform,
                Url = s.Url,
                Username = s.Username
            }).ToList() ?? []
        };
    }

    private async Task ValidateSaveBusinessAsync(long familyId, InputSaveMember request, long? memberId)
    {
        var selfError = _validation.ValidateSelfReference(memberId ?? 0, request.ParentId, request.SpouseId);
        if (selfError is not null)
        {
            throw new BadRequestException(selfError);
        }

        var rootError = _validation.ValidateRootMember(
            request.IsRoot,
            await HasRootMemberAsync(familyId, memberId));
        if (rootError is not null)
        {
            throw new BadRequestException(rootError);
        }

        if (request.ParentId.HasValue && !await ExistsInFamilyAsync(familyId, request.ParentId.Value))
        {
            throw new NotFoundException("Parent member not found.");
        }

        if (request.SpouseId.HasValue && !await ExistsInFamilyAsync(familyId, request.SpouseId.Value))
        {
            throw new NotFoundException("Spouse member not found.");
        }

        var circular = await _validation.ValidateCircularParentAsync(
            memberId ?? 0,
            request.ParentId,
            GetParentIdAsync);
        if (circular is not null)
        {
            throw new BadRequestException(circular);
        }
    }
}
