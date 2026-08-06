using Dapper;
using MidnightApi.Data;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Models.Api;
using MidnightApi.Services;
using Npgsql;

namespace MidnightApi.Repositories;

public class MembersRepository : IMembersRepository
{
    private readonly IDbConnectionFactory _connections;
    private readonly MemberValidationService _validation;

    public MembersRepository(IDbConnectionFactory connections, MemberValidationService validation)
    {
        _connections = connections;
        _validation = validation;
    }

    public async Task<OutputPagedMembers> GetListAsync(long familyId, InputMemberListQuery query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        var rows = (await connection.QueryAsync<MemberListRow>(
            StoredProcedures.MemberList,
            new
            {
                p_family_id = familyId,
                p_search = query.Search,
                p_gender = query.Gender,
                p_sort_by = query.SortBy,
                p_sort_desc = query.SortDesc,
                p_page = page,
                p_page_size = pageSize
            })).ToList();

        var total = rows.FirstOrDefault()?.TotalCount ?? 0;
        return new OutputPagedMembers
        {
            Items = rows.Select(r => new OutputMemberListItem
            {
                Id = r.Id,
                FirstName = r.FirstName,
                LastName = r.LastName,
                FullName = r.FullName,
                Gender = r.Gender,
                DateOfBirth = r.DateOfBirth,
                IsRoot = r.IsRoot,
                Profession = r.Profession,
                PhotoUrl = r.PhotoUrl
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = (int)total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<OutputFamilyTree> GetTreeAsync(long familyId)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        var rows = (await connection.QueryAsync<MemberTreeRow>(
            StoredProcedures.MemberTree,
            new { p_family_id = familyId })).ToList();

        var totalMembers = rows.FirstOrDefault()?.TotalMembers ?? 0;
        var rootRow = rows.FirstOrDefault(r => r.IsRoot);
        if (rootRow is null)
        {
            return new OutputFamilyTree { Root = null, TotalMembers = totalMembers };
        }

        var byId = rows.ToDictionary(r => r.Id);
        var childrenMap = rows
            .Where(r => r.ParentId.HasValue)
            .GroupBy(r => r.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        OutputTreeNode Build(MemberTreeRow row, bool includeChildren)
        {
            OutputTreeNode? spouse = null;
            if (row.SpouseId.HasValue && byId.TryGetValue(row.SpouseId.Value, out var spouseRow))
            {
                spouse = Build(spouseRow, includeChildren: false);
            }

            var children = includeChildren && childrenMap.TryGetValue(row.Id, out var kids)
                ? kids.Select(k => Build(k, includeChildren: true)).ToList()
                : [];

            return new OutputTreeNode
            {
                Id = row.Id,
                FirstName = row.FirstName,
                LastName = row.LastName,
                FullName = row.FullName,
                Gender = row.Gender,
                DateOfBirth = row.DateOfBirth,
                DateOfDeath = row.DateOfDeath,
                IsRoot = row.IsRoot,
                Nickname = row.Nickname,
                PhotoUrl = row.PhotoUrl,
                Spouse = spouse,
                Children = children
            };
        }

        return new OutputFamilyTree
        {
            Root = Build(rootRow, includeChildren: true),
            TotalMembers = totalMembers
        };
    }

    public async Task<OutputMemberProfile?> GetProfileAsync(long familyId, long memberId)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<MemberProfileRow>(
            StoredProcedures.MemberSelect,
            new { p_family_id = familyId, p_member_id = memberId });

        return row is null ? null : MapProfile(row);
    }

    public async Task<OutputMemberProfile> CreateAsync(long familyId, InputCreateMember input, string createdBy)
    {
        await ValidateSaveBusinessAsync(familyId, input, memberId: null);

        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        var memberId = await connection.ExecuteScalarAsync<long>(
            StoredProcedures.MemberInsert,
            new
            {
                p_family_id = familyId,
                p_parent_id = input.ParentId,
                p_spouse_id = input.SpouseId,
                p_first_name = input.FirstName,
                p_last_name = input.LastName,
                p_email = input.Email,
                p_phone = input.Phone,
                p_gender = input.Gender,
                p_dob = input.DateOfBirth,
                p_dod = input.DateOfDeath,
                p_is_root = input.IsRoot,
                p_nickname = input.Nickname,
                p_biography = input.Biography,
                p_profession = input.Profession,
                p_addresses = DapperExtensions.ToJsonb(input.Addresses),
                p_images = DapperExtensions.ToJsonb(input.Images),
                p_events = DapperExtensions.ToJsonb(input.Events),
                p_notes = DapperExtensions.ToJsonb(input.Notes),
                p_social_links = DapperExtensions.ToJsonb(input.SocialLinks),
                p_created_by = createdBy
            });

        return BuildCreateResponse(memberId, input);
    }

    public async Task<OutputMemberProfile?> UpdateAsync(long familyId, long memberId, InputUpdateMember input, string updatedBy)
    {
        await ValidateSaveBusinessAsync(familyId, input, memberId);

        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        try
        {
            var updated = await connection.ExecuteScalarAsync<bool>(
                StoredProcedures.MemberUpdate,
                new
                {
                    p_family_id = familyId,
                    p_member_id = memberId,
                    p_parent_id = input.ParentId,
                    p_spouse_id = input.SpouseId,
                    p_first_name = input.FirstName,
                    p_last_name = input.LastName,
                    p_email = input.Email,
                    p_phone = input.Phone,
                    p_gender = input.Gender,
                    p_dob = input.DateOfBirth,
                    p_dod = input.DateOfDeath,
                    p_is_root = input.IsRoot,
                    p_nickname = input.Nickname,
                    p_biography = input.Biography,
                    p_profession = input.Profession,
                    p_addresses = DapperExtensions.ToJsonb(input.Addresses),
                    p_images = DapperExtensions.ToJsonb(input.Images),
                    p_events = DapperExtensions.ToJsonb(input.Events),
                    p_notes = DapperExtensions.ToJsonb(input.Notes),
                    p_social_links = DapperExtensions.ToJsonb(input.SocialLinks),
                    p_updated_by = updatedBy
                });

            if (!updated)
            {
                return null;
            }
        }
        catch (PostgresException ex) when (ex.MessageText.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException(ex.MessageText);
        }

        return await GetProfileAsync(familyId, memberId);
    }

    public async Task<bool> SoftDeleteAsync(long familyId, long memberId, string deletedBy)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        var result = await connection.ExecuteScalarAsync<int>(
            StoredProcedures.MemberDelete,
            new { p_family_id = familyId, p_member_id = memberId, p_deleted_by = deletedBy });

        if (result == -1)
        {
            throw new BadRequestException(
                "Cannot delete this member because they have children. Remove or reassign children first.");
        }

        return result == 1;
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

        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        await connection.ExecuteAsync(
            StoredProcedures.MemberMapSpouse,
            new
            {
                p_family_id = familyId,
                p_member_id = memberId,
                p_spouse_id = spouseId,
                p_updated_by = updatedBy
            });
    }

    public async Task<OutputDashboard> GetDashboardAsync(long familyId)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        DashboardRow row;
        try
        {
            row = await connection.QuerySingleAsync<DashboardRow>(
                StoredProcedures.MemberDashboard,
                new { p_family_id = familyId });
        }
        catch (PostgresException ex) when (ex.MessageText.Contains("Family not found", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException("Family not found.");
        }

        var family = DapperExtensions.FromJsonb<OutputGetFamily>(row.FamilyJson)
            ?? throw new NotFoundException("Family not found.");
        var recent = DapperExtensions.FromJsonb<List<OutputMemberListItem>>(row.RecentMembersJson) ?? [];
        var birthdaySource = DapperExtensions.FromJsonb<List<BirthdaySource>>(row.MembersForBirthdayJson) ?? [];
        var stats = DapperExtensions.FromJsonb<OutputDashboardStats>(row.StatsJson) ?? new OutputDashboardStats();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcoming = birthdaySource
            .Where(m => m.DateOfBirth.HasValue)
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
                    MemberId = m.MemberId,
                    FullName = m.FullName,
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
            Family = family,
            TotalMembers = row.TotalMembers,
            TotalGenerations = row.TotalGenerations,
            RecentMembers = recent,
            UpcomingBirthdays = upcoming,
            Stats = stats
        };
    }

    public async Task<List<OutputTimelineItem>> GetTimelineAsync(long familyId)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<OutputTimelineItem>(
            StoredProcedures.MemberTimeline,
            new { p_family_id = familyId });
        return rows.ToList();
    }

    public async Task<bool> ExistsInFamilyAsync(long familyId, long memberId)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            StoredProcedures.MemberExistsInFamily,
            new { p_family_id = familyId, p_member_id = memberId });
    }

    public async Task<bool> HasRootMemberAsync(long familyId, long? excludeMemberId = null)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            StoredProcedures.MemberHasRoot,
            new { p_family_id = familyId, p_exclude_member_id = excludeMemberId });
    }

    public async Task<long?> GetParentIdAsync(long memberId)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        return await connection.ExecuteScalarAsync<long?>(
            StoredProcedures.MemberGetParentId,
            new { p_member_id = memberId });
    }

    public async Task<(long? ParentId, long? SpouseId)?> GetRelationAsync(long familyId, long memberId)
    {
        await using var connection = (NpgsqlConnection)await _connections.CreateOpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<RelationRow>(
            StoredProcedures.MemberGetRelation,
            new { p_family_id = familyId, p_member_id = memberId });

        return row is null ? null : (row.ParentId, row.SpouseId);
    }

    private async Task ValidateSaveBusinessAsync(long familyId, InputMemberSaveBase request, long? memberId)
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

    private static OutputMemberProfile MapProfile(MemberProfileRow row) => new()
    {
        Id = row.Id,
        FirstName = row.FirstName,
        LastName = row.LastName,
        FullName = row.FullName,
        Email = row.Email,
        Phone = row.Phone,
        Gender = row.Gender,
        DateOfBirth = row.DateOfBirth,
        DateOfDeath = row.DateOfDeath,
        IsRoot = row.IsRoot,
        Nickname = row.Nickname,
        Biography = row.Biography,
        Profession = row.Profession,
        Parent = DapperExtensions.FromJsonb<MemberRelationSummary>(row.ParentJson),
        Spouse = DapperExtensions.FromJsonb<MemberRelationSummary>(row.SpouseJson),
        Children = DapperExtensions.FromJsonb<List<MemberRelationSummary>>(row.ChildrenJson) ?? [],
        Addresses = DapperExtensions.FromJsonb<List<MemberAddressItem>>(row.AddressesJson) ?? [],
        Images = DapperExtensions.FromJsonb<List<MemberImageItem>>(row.ImagesJson) ?? [],
        Events = DapperExtensions.FromJsonb<List<MemberEventItem>>(row.EventsJson) ?? [],
        Notes = DapperExtensions.FromJsonb<List<MemberNoteItem>>(row.NotesJson) ?? [],
        SocialLinks = DapperExtensions.FromJsonb<List<MemberSocialLinkItem>>(row.SocialLinksJson) ?? []
    };

    private static OutputMemberProfile BuildCreateResponse(long memberId, InputCreateMember input) => new()
    {
        Id = memberId,
        FirstName = input.FirstName,
        LastName = input.LastName,
        FullName = $"{input.FirstName} {input.LastName}",
        Email = input.Email,
        Phone = input.Phone,
        Gender = input.Gender,
        DateOfBirth = input.DateOfBirth,
        DateOfDeath = input.DateOfDeath,
        IsRoot = input.IsRoot,
        Nickname = input.Nickname,
        Biography = input.Biography,
        Profession = input.Profession,
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

    private sealed class MemberListRow
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public bool IsRoot { get; set; }
        public string? Profession { get; set; }
        public string? PhotoUrl { get; set; }
        public long TotalCount { get; set; }
    }

    private sealed class MemberTreeRow
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateOnly? DateOfDeath { get; set; }
        public bool IsRoot { get; set; }
        public string? Nickname { get; set; }
        public string? PhotoUrl { get; set; }
        public long? ParentId { get; set; }
        public long? SpouseId { get; set; }
        public int TotalMembers { get; set; }
    }

    private sealed class MemberProfileRow
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateOnly? DateOfDeath { get; set; }
        public bool IsRoot { get; set; }
        public string? Nickname { get; set; }
        public string? Biography { get; set; }
        public string? Profession { get; set; }
        public object? ParentJson { get; set; }
        public object? SpouseJson { get; set; }
        public object? ChildrenJson { get; set; }
        public object? AddressesJson { get; set; }
        public object? ImagesJson { get; set; }
        public object? EventsJson { get; set; }
        public object? NotesJson { get; set; }
        public object? SocialLinksJson { get; set; }
    }

    private sealed class DashboardRow
    {
        public object? FamilyJson { get; set; }
        public int TotalMembers { get; set; }
        public int TotalGenerations { get; set; }
        public object? RecentMembersJson { get; set; }
        public object? MembersForBirthdayJson { get; set; }
        public object? StatsJson { get; set; }
    }

    private sealed class BirthdaySource
    {
        public long MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateOnly? DateOfBirth { get; set; }
    }

    private sealed class RelationRow
    {
        public long? ParentId { get; set; }
        public long? SpouseId { get; set; }
    }
}
