using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Exceptions;
using MidnightApi.Models.Api;
using MidnightApi.Models.Entities;

namespace MidnightApi.Repositories.Managers;

public class MembersRepositoryManager
{
    private readonly DbConnectionClass _db;

    public MembersRepositoryManager(DbConnectionClass db)
    {
        _db = db;
    }

    public Task<Member?> LoadProfileEntityAsync(long familyId, long memberId)
    {
        return _db.Members
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

    public Task<Member> CreateMemberAsync(long familyId, InputMemberSaveBase input, string createdBy)
    {
        var member = MapToEntity(familyId, input, createdBy);
        _db.Members.Add(member);
        return Task.FromResult(member);
    }

    public Task SaveAddressesAsync(long memberId, List<InputCreateMemberAddress>? addresses, string createdBy)
    {
        if (addresses is null || addresses.Count == 0)
        {
            return Task.CompletedTask;
        }

        var rows = addresses.Select(address => new MemberAddress
        {
            FK_Members = memberId,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            State = address.State,
            Country = address.Country,
            PostalCode = address.PostalCode,
            IsPrimary = address.IsPrimary,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        });

        _db.MemberAddresses.AddRange(rows);
        return Task.CompletedTask;
    }

    public Task SaveImagesAsync(long memberId, List<InputCreateMemberImage>? images, string createdBy)
    {
        if (images is null || images.Count == 0)
        {
            return Task.CompletedTask;
        }

        var rows = images.Select(image => new MemberImage
        {
            FK_Members = memberId,
            ImageUrl = image.ImageUrl,
            Caption = image.Caption,
            IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        });

        _db.MemberImages.AddRange(rows);
        return Task.CompletedTask;
    }

    public Task SaveEventsAsync(long memberId, List<InputCreateMemberEvent>? events, string createdBy)
    {
        if (events is null || events.Count == 0)
        {
            return Task.CompletedTask;
        }

        var rows = events.Select(memberEvent => new MemberEvent
        {
            FK_Members = memberId,
            EventType = memberEvent.EventType,
            Title = memberEvent.Title,
            Description = memberEvent.Description,
            EventDate = memberEvent.EventDate,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        });

        _db.MemberEvents.AddRange(rows);
        return Task.CompletedTask;
    }

    public Task SaveNotesAsync(long memberId, List<InputCreateMemberNote>? notes, string createdBy)
    {
        if (notes is null || notes.Count == 0)
        {
            return Task.CompletedTask;
        }

        var rows = notes.Select(note => new MemberNote
        {
            FK_Members = memberId,
            Title = note.Title,
            Content = note.Content,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        });

        _db.MemberNotes.AddRange(rows);
        return Task.CompletedTask;
    }

    public Task SaveSocialLinksAsync(long memberId, List<InputCreateMemberSocialLink>? socialLinks, string createdBy)
    {
        if (socialLinks is null || socialLinks.Count == 0)
        {
            return Task.CompletedTask;
        }

        var rows = socialLinks.Select(link => new MemberSocialLink
        {
            FK_Members = memberId,
            Platform = link.Platform,
            Url = link.Url,
            Username = link.Username,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        });

        _db.MemberSocialLinks.AddRange(rows);
        return Task.CompletedTask;
    }

    public async Task LinkSpouseAsync(Member member, long? spouseId, string actor)
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

    public void ApplyBasicFields(Member existing, InputMemberSaveBase input, string updatedBy)
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

    public async Task ApplySpouseLinkAsync(Member member, long? spouseId, string actor)
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

    public async Task SyncNestedAsync(long memberId, InputUpdateMember input, string actor, bool replaceMissing)
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

    public async Task<int> CountGenerationsAsync(long familyId)
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

    public async Task LoadSpouseDetailsAsync(Member member)
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

    public async Task LoadChildrenRecursiveAsync(Member parent)
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

    public async Task<OutputMemberProfile> MapToProfileAsync(Member member)
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

    public OutputMemberListItem MapToListItem(Member m) => new()
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

    public OutputTreeNode MapToTreeNode(Member m) => new()
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

    public Member MapToEntity(long familyId, InputMemberSaveBase input, string createdBy) => new()
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

    private async Task SyncAddressesAsync(long memberId, List<InputUpdateMemberAddress> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberAddresses
            .Where(a => a.FK_Members == memberId && !a.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id is > 0)
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
            var keep = items.Where(i => i.Id is > 0).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(a => !keep.Contains(a.ID_MemberAddresses)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private async Task SyncImagesAsync(long memberId, List<InputUpdateMemberImage> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberImages
            .Where(i => i.FK_Members == memberId && !i.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id is > 0)
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
            var keep = items.Where(i => i.Id is > 0).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(i => !keep.Contains(i.ID_MemberImages)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private async Task SyncEventsAsync(long memberId, List<InputUpdateMemberEvent> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberEvents
            .Where(e => e.FK_Members == memberId && !e.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id is > 0)
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
            var keep = items.Where(i => i.Id is > 0).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(e => !keep.Contains(e.ID_MemberEvents)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private async Task SyncNotesAsync(long memberId, List<InputUpdateMemberNote> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberNotes
            .Where(n => n.FK_Members == memberId && !n.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id is > 0)
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
            var keep = items.Where(i => i.Id is > 0).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(n => !keep.Contains(n.ID_MemberNotes)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private async Task SyncSocialLinksAsync(long memberId, List<InputUpdateMemberSocialLink> items, string actor, bool replaceMissing)
    {
        var existing = await _db.MemberSocialLinks
            .Where(s => s.FK_Members == memberId && !s.IsCancelled)
            .ToListAsync();

        foreach (var item in items)
        {
            if (item.Id is > 0)
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
            var keep = items.Where(i => i.Id is > 0).Select(i => i.Id!.Value).ToHashSet();
            foreach (var row in existing.Where(s => !keep.Contains(s.ID_MemberSocialLinks)))
            {
                SoftCancel(row, actor);
            }
        }
    }

    private OutputTreeNode MapToTreeNodeFlat(Member m) => new()
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

    private static void SoftCancel(AuditableEntity entity, string actor)
    {
        entity.IsCancelled = true;
        entity.CancelledBy = actor;
        entity.CancelledOn = DateTime.UtcNow;
    }
}
