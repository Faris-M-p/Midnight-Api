using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Models.Api;

public class MemberAddressItem
{
    public long Id { get; set; }
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
    public long Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class MemberEventItem
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
}

public class MemberNoteItem
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class MemberSocialLinkItem
{
    public long Id { get; set; }
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

public class InputMemberListQuery
{
    [StringLength(200)]
    public string? Search { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    [StringLength(30)]
    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class InputMemberRouteRequest
{
    [Display(Name = "Member Id")]
    [FromRoute(Name = "id")]
    [GreaterThanZero]
    public long Id { get; set; }
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

public abstract class InputMemberSaveBase
{
    [Display(Name = "First Name")]
    [Required, StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last Name")]
    [Required, StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [EmailAddress, StringLength(256)]
    [TrimmedString]
    public string? Email { get; set; }

    [Display(Name = "Phone Number")]
    [PhoneNumber, StringLength(30)]
    [TrimmedString]
    public string? Phone { get; set; }

    [Display(Name = "Gender")]
    [StringLength(20)]
    [System.ComponentModel.DataAnnotations.AllowedValues("Male", "Female", "Other")]
    [TrimmedString]
    public string? Gender { get; set; }

    [Display(Name = "Date Of Birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Display(Name = "Date Of Death")]
    [DateRange(nameof(DateOfBirth), nameof(DateOfDeath), ErrorMessage = "Date of death must be after or equal to date of birth.")]
    public DateOnly? DateOfDeath { get; set; }

    public bool IsRoot { get; set; }

    [StringLength(4000)]
    [NoScriptTags]
    public string? Biography { get; set; }

    [StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string? Profession { get; set; }

    [Display(Name = "Parent Id")]
    [GreaterThanZero]
    public long? ParentId { get; set; }

    [Display(Name = "Spouse Id")]
    [GreaterThanZero]
    public long? SpouseId { get; set; }
}

public class InputCreateMemberAddress
{
    [Required, StringLength(300)]
    [TrimmedString]
    [NoScriptTags]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(300)]
    public string? AddressLine2 { get; set; }

    [Required, StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string? State { get; set; }

    [Required, StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string Country { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PostalCode { get; set; }

    public bool IsPrimary { get; set; }
}

public class InputUpdateMemberAddress : InputCreateMemberAddress
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMemberImage
{
    [Required, Url, StringLength(2000)]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Caption { get; set; }

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class InputUpdateMemberImage : InputCreateMemberImage
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMemberEvent
{
    [Required, StringLength(50)]
    [TrimmedString]
    [NoScriptTags]
    public string EventType { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public DateOnly EventDate { get; set; }
}

public class InputUpdateMemberEvent : InputCreateMemberEvent
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMemberNote
{
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    [NoScriptTags]
    public string Content { get; set; } = string.Empty;
}

public class InputUpdateMemberNote : InputCreateMemberNote
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMemberSocialLink
{
    [Required, StringLength(50)]
    [TrimmedString]
    [NoScriptTags]
    public string Platform { get; set; } = string.Empty;

    [Required, Url, StringLength(2000)]
    public string Url { get; set; } = string.Empty;

    [StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string? Username { get; set; }
}

public class InputUpdateMemberSocialLink : InputCreateMemberSocialLink
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMember : InputMemberSaveBase
{
    public List<InputCreateMemberAddress>? Addresses { get; set; }
    public List<InputCreateMemberImage>? Images { get; set; }
    public List<InputCreateMemberEvent>? Events { get; set; }
    public List<InputCreateMemberNote>? Notes { get; set; }
    public List<InputCreateMemberSocialLink>? SocialLinks { get; set; }
}

public class InputUpdateMember : InputMemberSaveBase
{
    public List<InputUpdateMemberAddress>? Addresses { get; set; }
    public List<InputUpdateMemberImage>? Images { get; set; }
    public List<InputUpdateMemberEvent>? Events { get; set; }
    public List<InputUpdateMemberNote>? Notes { get; set; }
    public List<InputUpdateMemberSocialLink>? SocialLinks { get; set; }
}

public class InputMapSpouse
{
    [Display(Name = "Member Id")]
    [GreaterThanZero]
    public long MemberId { get; set; }

    [Display(Name = "Spouse Id")]
    [GreaterThanZero]
    public long SpouseId { get; set; }
}

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
