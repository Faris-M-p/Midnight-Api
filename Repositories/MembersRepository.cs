using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Exceptions;
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
            Items = rows.Select(MapToListItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<OutputMemberProfile?> GetProfileAsync(long familyId, long memberId)
    {
        var member = await LoadProfileEntityAsync(familyId, memberId);
        return member is null ? null : await MapToProfileAsync(member);
    }

    public async Task<OutputMemberProfile> CreateAsync(long familyId, InputSaveMember input, string createdBy)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var member = MapToEntity(familyId, input, createdBy);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        await SyncNestedAsync(member.ID_Members, input, createdBy, replaceMissing: false);
        await ApplySpouseLinkAsync(member, input.SpouseId, createdBy);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return (await GetProfileAsync(familyId, member.ID_Members))!;
    }

    public async Task<OutputMemberProfile?> UpdateAsync(long familyId, long memberId, InputSaveMember input, string updatedBy)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var existing = await _db.Members
            .FirstOrDefaultAsync(m => m.ID_Members == memberId && m.FK_Families == familyId && !m.IsCancelled);
        if (existing is null)
        {
            return null;
        }

        ApplyBasicFields(existing, input, updatedBy);
        await SyncNestedAsync(memberId, input, updatedBy, replaceMissing: true);
        await ApplySpouseLinkAsync(existing, input.SpouseId, updatedBy);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetProfileAsync(familyId, memberId);
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

        await using var tx = await _db.Database.BeginTransactionAsync();
        var spouse = MapToEntity(familyId, spouseInput, createdBy);
        _db.Members.Add(spouse);
        await _db.SaveChangesAsync();

        await SyncNestedAsync(spouse.ID_Members, spouseInput, createdBy, replaceMissing: false);

        member.FK_Members_Spouse = spouse.ID_Members;
        spouse.FK_Members_Spouse = member.ID_Members;
        member.UpdatedBy = createdBy;
        member.UpdatedOn = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return (await GetProfileAsync(familyId, spouse.ID_Members))!;
    }

    public async Task MapSpouseAsync(long familyId, long memberId, long spouseId, string updatedBy)
    {
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

        await LoadSpouseDetailsAsync(root);
        await LoadChildrenRecursiveAsync(root);

        return new OutputFamilyTree
        {
            Root = MapToTreeNode(root),
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
            TotalGenerations = await CountGenerationsAsync(familyId),
            RecentMembers = members
                .OrderByDescending(m => m.CreatedOn)
                .Take(5)
                .Select(MapToListItem)
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

    private async Task<int> CountGenerationsAsync(long familyId)
    {
        var root = await _db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FK_Families == familyId && m.IsRoot && !m.IsCancelled);
        if (root is null)
        {
            return 0;
        }

        var all = await _db.Members
            .AsNoTracking()
            .Where(m => m.FK_Families == familyId && !m.IsCancelled)
            .Select(m => new { m.ID_Members, m.FK_Members_Parent })
            .ToListAsync();

        var childrenMap = all
            .Where(m => m.FK_Members_Parent.HasValue)
            .GroupBy(m => m.FK_Members_Parent!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ID_Members).ToList());

        var maxDepth = 1;
        void Walk(long id, int depth)
        {
            maxDepth = Math.Max(maxDepth, depth);
            if (!childrenMap.TryGetValue(id, out var kids))
            {
                return;
            }

            foreach (var kid in kids)
            {
                Walk(kid, depth + 1);
            }
        }

        Walk(root.ID_Members, 1);
        return maxDepth;
    }

    private async Task ApplySpouseLinkAsync(Member member, long? spouseId, string actor)
    {
        if (!spouseId.HasValue)
        {
            return;
        }

        var spouse = await _db.Members
            .FirstOrDefaultAsync(m =>
                m.ID_Members == spouseId.Value
                && m.FK_Families == member.FK_Families
                && !m.IsCancelled)
            ?? throw new NotFoundException("Spouse member not found.");

        member.FK_Members_Spouse = spouse.ID_Members;
        spouse.FK_Members_Spouse = member.ID_Members;
        spouse.UpdatedBy = actor;
        spouse.UpdatedOn = DateTime.UtcNow;
    }

    private async Task SyncNestedAsync(long memberId, InputSaveMember input, string actor, bool replaceMissing)
    {
        if (input.Addresses is not null)
        {
            await SyncAddressesAsync(memberId, input.Addresses, actor, replaceMissing);
        }

        if (input.Images is not null)
        {
            await SyncImagesAsync(memberId, input.Images, actor, replaceMissing);
        }

        if (input.Events is not null)
        {
            await SyncEventsAsync(memberId, input.Events, actor, replaceMissing);
        }

        if (input.Notes is not null)
        {
            await SyncNotesAsync(memberId, input.Notes, actor, replaceMissing);
        }

        if (input.SocialLinks is not null)
        {
            await SyncSocialLinksAsync(memberId, input.SocialLinks, actor, replaceMissing);
        }
    }

    private async Task SyncAddressesAsync(long memberId, List<MemberAddressItem> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberAddresses
            .Where(a => a.FK_Members == memberId && !a.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id.HasValue)
            {
                var row = existing.FirstOrDefault(a => a.ID_MemberAddresses == item.Id.Value)
                    ?? throw new NotFoundException("Address not found.");
                row.AddressLine1 = item.AddressLine1;
                row.AddressLine2 = item.AddressLine2;
                row.City = item.City;
                row.State = item.State;
                row.Country = item.Country;
                row.PostalCode = item.PostalCode;
                row.IsPrimary = item.IsPrimary;
                row.UpdatedBy = actor;
                row.UpdatedOn = DateTime.UtcNow;
            }
            else
            {
                _db.MemberAddresses.Add(new MemberAddress
                {
                    FK_Members = memberId,
                    AddressLine1 = item.AddressLine1,
                    AddressLine2 = item.AddressLine2,
                    City = item.City,
                    State = item.State,
                    Country = item.Country,
                    PostalCode = item.PostalCode,
                    IsPrimary = item.IsPrimary,
                    CreatedBy = actor,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        if (replaceMissing)
        {
            var keep = items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(a => !keep.Contains(a.ID_MemberAddresses)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private async Task SyncImagesAsync(long memberId, List<MemberImageItem> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberImages
            .Where(i => i.FK_Members == memberId && !i.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id.HasValue)
            {
                var row = existing.FirstOrDefault(i => i.ID_MemberImages == item.Id.Value)
                    ?? throw new NotFoundException("Image not found.");
                row.ImageUrl = item.ImageUrl;
                row.Caption = item.Caption;
                row.IsPrimary = item.IsPrimary;
                row.SortOrder = item.SortOrder;
                row.UpdatedBy = actor;
                row.UpdatedOn = DateTime.UtcNow;
            }
            else
            {
                _db.MemberImages.Add(new MemberImage
                {
                    FK_Members = memberId,
                    ImageUrl = item.ImageUrl,
                    Caption = item.Caption,
                    IsPrimary = item.IsPrimary,
                    SortOrder = item.SortOrder,
                    CreatedBy = actor,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        if (replaceMissing)
        {
            var keep = items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(i => !keep.Contains(i.ID_MemberImages)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private async Task SyncEventsAsync(long memberId, List<MemberEventItem> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberEvents
            .Where(e => e.FK_Members == memberId && !e.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id.HasValue)
            {
                var row = existing.FirstOrDefault(e => e.ID_MemberEvents == item.Id.Value)
                    ?? throw new NotFoundException("Event not found.");
                row.EventType = item.EventType;
                row.Title = item.Title;
                row.Description = item.Description;
                row.EventDate = item.EventDate;
                row.UpdatedBy = actor;
                row.UpdatedOn = DateTime.UtcNow;
            }
            else
            {
                _db.MemberEvents.Add(new MemberEvent
                {
                    FK_Members = memberId,
                    EventType = item.EventType,
                    Title = item.Title,
                    Description = item.Description,
                    EventDate = item.EventDate,
                    CreatedBy = actor,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        if (replaceMissing)
        {
            var keep = items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(e => !keep.Contains(e.ID_MemberEvents)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private async Task SyncNotesAsync(long memberId, List<MemberNoteItem> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberNotes
            .Where(n => n.FK_Members == memberId && !n.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id.HasValue)
            {
                var row = existing.FirstOrDefault(n => n.ID_MemberNotes == item.Id.Value)
                    ?? throw new NotFoundException("Note not found.");
                row.Title = item.Title;
                row.Content = item.Content;
                row.UpdatedBy = actor;
                row.UpdatedOn = DateTime.UtcNow;
            }
            else
            {
                _db.MemberNotes.Add(new MemberNote
                {
                    FK_Members = memberId,
                    Title = item.Title,
                    Content = item.Content,
                    CreatedBy = actor,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        if (replaceMissing)
        {
            var keep = items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(n => !keep.Contains(n.ID_MemberNotes)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private async Task SyncSocialLinksAsync(long memberId, List<MemberSocialLinkItem> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberSocialLinks
            .Where(s => s.FK_Members == memberId && !s.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id.HasValue)
            {
                var row = existing.FirstOrDefault(s => s.ID_MemberSocialLinks == item.Id.Value)
                    ?? throw new NotFoundException("Social link not found.");
                row.Platform = item.Platform;
                row.Url = item.Url;
                row.Username = item.Username;
                row.UpdatedBy = actor;
                row.UpdatedOn = DateTime.UtcNow;
            }
            else
            {
                _db.MemberSocialLinks.Add(new MemberSocialLink
                {
                    FK_Members = memberId,
                    Platform = item.Platform,
                    Url = item.Url,
                    Username = item.Username,
                    CreatedBy = actor,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        if (replaceMissing)
        {
            var keep = items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(s => !keep.Contains(s.ID_MemberSocialLinks)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private static void SoftCancel(AuditableEntity entity, string actor)
    {
        entity.IsCancelled = true;
        entity.CancelledBy = actor;
        entity.CancelledOn = DateTime.UtcNow;
    }

    private async Task<Member?> LoadProfileEntityAsync(long familyId, long memberId)
    {
        return await _db.Members
            .Where(m => m.ID_Members == memberId && m.FK_Families == familyId && !m.IsCancelled)
            .Include(m => m.Addresses.Where(a => !a.IsCancelled))
            .Include(m => m.Images.Where(i => !i.IsCancelled))
            .Include(m => m.Events.Where(e => !e.IsCancelled))
            .Include(m => m.SocialLinks.Where(s => !s.IsCancelled))
            .Include(m => m.Notes.Where(n => !n.IsCancelled))
            .Include(m => m.Parent)
            .Include(m => m.Spouse)
            .Include(m => m.Children.Where(c => !c.IsCancelled))
            .FirstOrDefaultAsync();
    }

    private async Task LoadSpouseDetailsAsync(Member member)
    {
        if (!member.FK_Members_Spouse.HasValue)
        {
            return;
        }

        member.Spouse = await _db.Members
            .Where(m => m.ID_Members == member.FK_Members_Spouse && !m.IsCancelled)
            .Include(m => m.Images.Where(i => !i.IsCancelled))
            .FirstOrDefaultAsync();
    }

    private async Task LoadChildrenRecursiveAsync(Member parent)
    {
        var children = await _db.Members
            .Where(m => m.FK_Members_Parent == parent.ID_Members && !m.IsCancelled)
            .Include(m => m.Images.Where(i => !i.IsCancelled))
            .ToListAsync();

        parent.Children = children;
        foreach (var child in children)
        {
            await LoadSpouseDetailsAsync(child);
            await LoadChildrenRecursiveAsync(child);
        }
    }

    private async Task<OutputMemberProfile> MapToProfileAsync(Member member)
    {
        string? Photo(Member? m) =>
            m?.Images.Where(i => !i.IsCancelled).OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                .Select(i => i.ImageUrl).FirstOrDefault();

        MemberRelationSummary? Rel(Member? m) => m is null || m.IsCancelled
            ? null
            : new MemberRelationSummary
            {
                Id = m.ID_Members,
                FirstName = m.FirstName,
                LastName = m.LastName,
                FullName = $"{m.FirstName} {m.LastName}",
                Gender = m.Gender,
                DateOfBirth = m.DateOfBirth,
                PhotoUrl = Photo(m)
            };

        if (member.Parent is null && member.FK_Members_Parent.HasValue)
        {
            member.Parent = await _db.Members
                .Include(m => m.Images.Where(i => !i.IsCancelled))
                .FirstOrDefaultAsync(m => m.ID_Members == member.FK_Members_Parent);
        }

        if (member.Spouse is null && member.FK_Members_Spouse.HasValue)
        {
            member.Spouse = await _db.Members
                .Include(m => m.Images.Where(i => !i.IsCancelled))
                .FirstOrDefaultAsync(m => m.ID_Members == member.FK_Members_Spouse);
        }

        var children = member.Children.Count > 0
            ? member.Children.Where(c => !c.IsCancelled).ToList()
            : await _db.Members
                .Where(c => c.FK_Members_Parent == member.ID_Members && !c.IsCancelled)
                .Include(c => c.Images.Where(i => !i.IsCancelled))
                .ToListAsync();

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
            Parent = Rel(member.Parent),
            Spouse = Rel(member.Spouse),
            Children = children.Select(c => Rel(c)!).ToList(),
            Addresses = member.Addresses.Select(a => new MemberAddressItem
            {
                Id = a.ID_MemberAddresses,
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                City = a.City,
                State = a.State,
                Country = a.Country,
                PostalCode = a.PostalCode,
                IsPrimary = a.IsPrimary
            }).ToList(),
            Images = member.Images.OrderBy(i => i.SortOrder).Select(i => new MemberImageItem
            {
                Id = i.ID_MemberImages,
                ImageUrl = i.ImageUrl,
                Caption = i.Caption,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            }).ToList(),
            Events = member.Events.OrderByDescending(e => e.EventDate).Select(e => new MemberEventItem
            {
                Id = e.ID_MemberEvents,
                EventType = e.EventType,
                Title = e.Title,
                Description = e.Description,
                EventDate = e.EventDate
            }).ToList(),
            Notes = member.Notes.Select(n => new MemberNoteItem
            {
                Id = n.ID_MemberNotes,
                Title = n.Title,
                Content = n.Content
            }).ToList(),
            SocialLinks = member.SocialLinks.Select(s => new MemberSocialLinkItem
            {
                Id = s.ID_MemberSocialLinks,
                Platform = s.Platform,
                Url = s.Url,
                Username = s.Username
            }).ToList()
        };
    }

    private static OutputMemberListItem MapToListItem(Member m) => new()
    {
        Id = m.ID_Members,
        FirstName = m.FirstName,
        LastName = m.LastName,
        FullName = $"{m.FirstName} {m.LastName}",
        Gender = m.Gender,
        DateOfBirth = m.DateOfBirth,
        IsRoot = m.IsRoot,
        Profession = m.Profession,
        PhotoUrl = m.Images.Where(i => !i.IsCancelled)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => i.ImageUrl)
            .FirstOrDefault()
    };

    private static OutputTreeNode MapToTreeNode(Member m) => new()
    {
        Id = m.ID_Members,
        FirstName = m.FirstName,
        LastName = m.LastName,
        FullName = $"{m.FirstName} {m.LastName}",
        Gender = m.Gender,
        DateOfBirth = m.DateOfBirth,
        DateOfDeath = m.DateOfDeath,
        IsRoot = m.IsRoot,
        PhotoUrl = m.Images.Where(i => !i.IsCancelled)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => i.ImageUrl)
            .FirstOrDefault(),
        Spouse = m.Spouse is null || m.Spouse.IsCancelled ? null : MapToTreeNodeFlat(m.Spouse),
        Children = m.Children.Where(c => !c.IsCancelled).Select(MapToTreeNode).ToList()
    };

    private static OutputTreeNode MapToTreeNodeFlat(Member m) => new()
    {
        Id = m.ID_Members,
        FirstName = m.FirstName,
        LastName = m.LastName,
        FullName = $"{m.FirstName} {m.LastName}",
        Gender = m.Gender,
        DateOfBirth = m.DateOfBirth,
        DateOfDeath = m.DateOfDeath,
        IsRoot = m.IsRoot,
        PhotoUrl = m.Images.Where(i => !i.IsCancelled)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => i.ImageUrl)
            .FirstOrDefault()
    };

    private static Member MapToEntity(long familyId, InputSaveMember input, string createdBy) => new()
    {
        FK_Families = familyId,
        FK_Members_Parent = input.ParentId,
        FirstName = input.FirstName.Trim(),
        LastName = input.LastName.Trim(),
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

    private static void ApplyBasicFields(Member existing, InputSaveMember input, string updatedBy)
    {
        existing.FK_Members_Parent = input.ParentId;
        existing.FirstName = input.FirstName.Trim();
        existing.LastName = input.LastName.Trim();
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
    }
}
