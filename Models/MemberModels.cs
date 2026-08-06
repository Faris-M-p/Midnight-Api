using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.DataAccess;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Models;

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

// --- Frontend / API views ---

public class InputMemberListQueryView
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

public class InputMemberRouteRequestView
{
    [Display(Name = "Member Id")]
    [FromRoute(Name = "id")]
    [GreaterThanZero]
    public long Id { get; set; }
}

public abstract class InputMemberSaveBaseView
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

    [Display(Name = "Nickname")]
    [StringLength(100)]
    [TrimmedString]
    [NoScriptTags]
    public string? Nickname { get; set; }

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

public class InputCreateMemberAddressView
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

public class InputUpdateMemberAddressView : InputCreateMemberAddressView
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMemberImageView
{
    [Required, Url, StringLength(2000)]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Caption { get; set; }

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class InputUpdateMemberImageView : InputCreateMemberImageView
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMemberEventView
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

public class InputUpdateMemberEventView : InputCreateMemberEventView
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMemberNoteView
{
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    [NoScriptTags]
    public string Content { get; set; } = string.Empty;
}

public class InputUpdateMemberNoteView : InputCreateMemberNoteView
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMemberSocialLinkView
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

public class InputUpdateMemberSocialLinkView : InputCreateMemberSocialLinkView
{
    [GreaterThanZero]
    public long? Id { get; set; }
}

public class InputCreateMemberView : InputMemberSaveBaseView
{
    public List<InputCreateMemberAddressView>? Addresses { get; set; }
    public List<InputCreateMemberImageView>? Images { get; set; }
    public List<InputCreateMemberEventView>? Events { get; set; }
    public List<InputCreateMemberNoteView>? Notes { get; set; }
    public List<InputCreateMemberSocialLinkView>? SocialLinks { get; set; }
}

public class InputUpdateMemberView : InputMemberSaveBaseView
{
    public List<InputUpdateMemberAddressView>? Addresses { get; set; }
    public List<InputUpdateMemberImageView>? Images { get; set; }
    public List<InputUpdateMemberEventView>? Events { get; set; }
    public List<InputUpdateMemberNoteView>? Notes { get; set; }
    public List<InputUpdateMemberSocialLinkView>? SocialLinks { get; set; }
}

public class InputMapSpouseView
{
    [Display(Name = "Member Id")]
    [GreaterThanZero]
    public long MemberId { get; set; }

    [Display(Name = "Spouse Id")]
    [GreaterThanZero]
    public long SpouseId { get; set; }
}

// --- DB input models ---

public class InputMemberList
{
    [DbParam("p_family_id")]
    public long FamilyId { get; set; }

    [DbParam("p_search")]
    public string? Search { get; set; }

    [DbParam("p_gender")]
    public string? Gender { get; set; }

    [DbParam("p_sort_by")]
    public string? SortBy { get; set; }

    [DbParam("p_sort_desc")]
    public bool SortDesc { get; set; }

    [DbParam("p_page")]
    public int Page { get; set; } = 1;

    [DbParam("p_page_size")]
    public int PageSize { get; set; } = 20;
}

public class InputMemberTree
{
    [DbParam("p_family_id")]
    public long FamilyId { get; set; }
}

public class InputGetMember
{
    [DbParam("p_family_id")]
    public long FamilyId { get; set; }

    [DbParam("p_member_id")]
    public long MemberId { get; set; }
}

public class InputCreateMember
{
    [DbParam("p_family_id")]
    public long FamilyId { get; set; }

    [DbParam("p_parent_id")]
    public long? ParentId { get; set; }

    [DbParam("p_spouse_id")]
    public long? SpouseId { get; set; }

    [DbParam("p_first_name")]
    public string FirstName { get; set; } = string.Empty;

    [DbParam("p_last_name")]
    public string LastName { get; set; } = string.Empty;

    [DbParam("p_email")]
    public string? Email { get; set; }

    [DbParam("p_phone")]
    public string? Phone { get; set; }

    [DbParam("p_gender")]
    public string? Gender { get; set; }

    [DbParam("p_dob")]
    public DateOnly? DateOfBirth { get; set; }

    [DbParam("p_dod")]
    public DateOnly? DateOfDeath { get; set; }

    [DbParam("p_is_root")]
    public bool IsRoot { get; set; }

    [DbParam("p_nickname")]
    public string? Nickname { get; set; }

    [DbParam("p_biography")]
    public string? Biography { get; set; }

    [DbParam("p_profession")]
    public string? Profession { get; set; }

    [DbParam("p_addresses")]
    public string? Addresses { get; set; }

    [DbParam("p_images")]
    public string? Images { get; set; }

    [DbParam("p_events")]
    public string? Events { get; set; }

    [DbParam("p_notes")]
    public string? Notes { get; set; }

    [DbParam("p_social_links")]
    public string? SocialLinks { get; set; }

    [DbParam("p_created_by")]
    public string CreatedBy { get; set; } = string.Empty;
}

public class InputUpdateMember
{
    [DbParam("p_family_id")]
    public long FamilyId { get; set; }

    [DbParam("p_member_id")]
    public long MemberId { get; set; }

    [DbParam("p_parent_id")]
    public long? ParentId { get; set; }

    [DbParam("p_spouse_id")]
    public long? SpouseId { get; set; }

    [DbParam("p_first_name")]
    public string FirstName { get; set; } = string.Empty;

    [DbParam("p_last_name")]
    public string LastName { get; set; } = string.Empty;

    [DbParam("p_email")]
    public string? Email { get; set; }

    [DbParam("p_phone")]
    public string? Phone { get; set; }

    [DbParam("p_gender")]
    public string? Gender { get; set; }

    [DbParam("p_dob")]
    public DateOnly? DateOfBirth { get; set; }

    [DbParam("p_dod")]
    public DateOnly? DateOfDeath { get; set; }

    [DbParam("p_is_root")]
    public bool IsRoot { get; set; }

    [DbParam("p_nickname")]
    public string? Nickname { get; set; }

    [DbParam("p_biography")]
    public string? Biography { get; set; }

    [DbParam("p_profession")]
    public string? Profession { get; set; }

    [DbParam("p_addresses")]
    public string? Addresses { get; set; }

    [DbParam("p_images")]
    public string? Images { get; set; }

    [DbParam("p_events")]
    public string? Events { get; set; }

    [DbParam("p_notes")]
    public string? Notes { get; set; }

    [DbParam("p_social_links")]
    public string? SocialLinks { get; set; }

    [DbParam("p_updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class InputDeleteMember
{
    [DbParam("p_family_id")]
    public long FamilyId { get; set; }

    [DbParam("p_member_id")]
    public long MemberId { get; set; }

    [DbParam("p_deleted_by")]
    public string DeletedBy { get; set; } = string.Empty;
}

public class InputMapSpouse
{
    [DbParam("p_family_id")]
    public long FamilyId { get; set; }

    [DbParam("p_member_id")]
    public long MemberId { get; set; }

    [DbParam("p_spouse_id")]
    public long SpouseId { get; set; }

    [DbParam("p_updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class InputMemberDashboard
{
    [DbParam("p_family_id")]
    public long FamilyId { get; set; }
}

public class InputMemberTimeline
{
    [DbParam("p_family_id")]
    public long FamilyId { get; set; }
}

// --- Output models ---

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

public class OutputGetMember
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
    public MemberRelationSummary? Parent { get; set; }
    public MemberRelationSummary? Spouse { get; set; }
    public List<MemberRelationSummary> Children { get; set; } = [];
    public List<MemberAddressItem> Addresses { get; set; } = [];
    public List<MemberImageItem> Images { get; set; } = [];
    public List<MemberEventItem> Events { get; set; } = [];
    public List<MemberNoteItem> Notes { get; set; } = [];
    public List<MemberSocialLinkItem> SocialLinks { get; set; } = [];
}

public class OutputCreateMember : CommonResponse
{
}

public class OutputUpdateMember : CommonResponse
{
}

public class OutputDeleteMember : CommonResponse
{
}

public class OutputMapSpouse : CommonResponse
{
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
    public string? Nickname { get; set; }
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
