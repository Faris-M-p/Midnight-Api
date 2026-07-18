namespace MidnightApi.Models.Api;

// --- Nested profile parts (no FK_Members exposed to clients) ---

public class MemberAddressItem
{
    public long? Id { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; }
}

public class MemberImageItem
{
    public long? Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class MemberEventItem
{
    public long? Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
}

public class MemberNoteItem
{
    public long? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class MemberSocialLinkItem
{
    public long? Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Username { get; set; }
}

public class MemberRelationSummary
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? PhotoUrl { get; set; }
}

// --- List / query ---

public class InputMemberListQuery
{
    public string? Search { get; set; }
    public string? Gender { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class OutputMemberListItem
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
}

public class OutputPagedMembers
{
    public List<OutputMemberListItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

// --- Detail / create / update ---

public class OutputMemberProfile
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
    public string? Biography { get; set; }
    public string? Profession { get; set; }
    public MemberRelationSummary? Parent { get; set; }
    public MemberRelationSummary? Spouse { get; set; }
    public List<MemberRelationSummary> Children { get; set; } = [];
    public List<MemberAddressItem> Addresses { get; set; } = [];
    public List<MemberImageItem> Images { get; set; } = [];
    public List<MemberEventItem> Events { get; set; } = [];
    public List<MemberNoteItem> Notes { get; set; } = [];
    public List<MemberSocialLinkItem> SocialLinks { get; set; } = [];
}

public class InputSaveMember
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly? DateOfDeath { get; set; }
    public bool IsRoot { get; set; }
    public string? Biography { get; set; }
    public string? Profession { get; set; }
    public long? ParentId { get; set; }
    public long? SpouseId { get; set; }
    public List<MemberAddressItem>? Addresses { get; set; }
    public List<MemberImageItem>? Images { get; set; }
    public List<MemberEventItem>? Events { get; set; }
    public List<MemberNoteItem>? Notes { get; set; }
    public List<MemberSocialLinkItem>? SocialLinks { get; set; }
}

public class InputAddChild
{
    public InputSaveMember Child { get; set; } = new();
}

public class InputAddSpouse
{
    public InputSaveMember Spouse { get; set; } = new();
}

public class InputMapSpouse
{
    public long MemberId { get; set; }
    public long SpouseId { get; set; }
}

// --- Tree ---

public class OutputTreeNode
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly? DateOfDeath { get; set; }
    public bool IsRoot { get; set; }
    public string? PhotoUrl { get; set; }
    public OutputTreeNode? Spouse { get; set; }
    public List<OutputTreeNode> Children { get; set; } = [];
}

public class OutputFamilyTree
{
    public OutputTreeNode? Root { get; set; }
    public int TotalMembers { get; set; }
}

// --- Dashboard / Timeline ---

public class OutputDashboard
{
    public OutputGetFamily Family { get; set; } = null!;
    public int TotalMembers { get; set; }
    public int TotalGenerations { get; set; }
    public List<OutputMemberListItem> RecentMembers { get; set; } = [];
    public List<OutputUpcomingBirthday> UpcomingBirthdays { get; set; } = [];
    public OutputDashboardStats Stats { get; set; } = new();
}

public class OutputDashboardStats
{
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public int OtherGenderCount { get; set; }
    public int LivingCount { get; set; }
    public int DeceasedCount { get; set; }
}

public class OutputUpcomingBirthday
{
    public long MemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public int TurningAge { get; set; }
    public int DaysUntil { get; set; }
}

public class OutputTimelineItem
{
    public long EventId { get; set; }
    public long MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
}
