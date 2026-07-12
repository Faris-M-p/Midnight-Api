namespace MidnightApi.Models.Api;

using MidnightApi.Models.Api;

public class InputSearchMembers
{
    public string Name { get; set; } = string.Empty;
    public long? FamilyId { get; set; }
}

public class InputSearchMembersView
{
    public string Name { get; set; } = string.Empty;
    public long? FamilyId { get; set; }
}

public class InputGetFamilyMembers
{
    public long FamilyId { get; set; }
}

public class InputGetMembersByGeneration
{
    public long FamilyId { get; set; }
    public int Level { get; set; }
}

public class InputGetMemberTree
{
    public long FamilyId { get; set; }
}

public class InputCreateMember
{
    public long FK_Families { get; set; }
    public long? FK_Members_Parent { get; set; }
    public long? FK_Members_Spouse { get; set; }
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
}

public class InputUpdateMember
{
    public long? FK_Members_Parent { get; set; }
    public long? FK_Members_Spouse { get; set; }
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
}

public class InputCreateMemberView
{
    public long FK_Families { get; set; }
    public long? FK_Members_Parent { get; set; }
    public long? FK_Members_Spouse { get; set; }
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
}

public class InputUpdateMemberView
{
    public long? FK_Members_Parent { get; set; }
    public long? FK_Members_Spouse { get; set; }
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
}

public class InputAddSpouseView
{
    public long? ExistingSpouseId { get; set; }
    public InputCreateMemberView? NewSpouse { get; set; }
}

public class InputAddChildView
{
    public InputCreateMemberView Child { get; set; } = new();
}

public class InputCreateMemberAddress
{
    public long FK_Members { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; }
}

public class InputUpdateMemberAddress
{
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; }
}

public class InputCreateMemberAddressView
{
    public long FK_Members { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; }
}

public class InputUpdateMemberAddressView
{
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; }
}

public class InputCreateMemberImage
{
    public long FK_Members { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class InputUpdateMemberImage
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class InputCreateMemberImageView
{
    public long FK_Members { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class InputUpdateMemberImageView
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class InputCreateMemberEvent
{
    public long FK_Members { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
}

public class InputUpdateMemberEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
}

public class InputCreateMemberEventView
{
    public long FK_Members { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
}

public class InputUpdateMemberEventView
{
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
}

public class InputCreateMemberNote
{
    public long FK_Members { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class InputUpdateMemberNote
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class InputCreateMemberNoteView
{
    public long FK_Members { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class InputUpdateMemberNoteView
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class InputCreateMemberSocialLink
{
    public long FK_Members { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Username { get; set; }
}

public class InputUpdateMemberSocialLink
{
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Username { get; set; }
}

public class InputCreateMemberSocialLinkView
{
    public long FK_Members { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Username { get; set; }
}

public class InputUpdateMemberSocialLinkView
{
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Username { get; set; }
}

public class OutputGetMember
{
    public long ID_Members { get; set; }
    public long FK_Families { get; set; }
    public long? FK_Members_Parent { get; set; }
    public long? FK_Members_Spouse { get; set; }
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
}

public class OutputGetMemberProfile : OutputGetMember
{
    public List<OutputGetMemberAddress> Addresses { get; set; } = [];
    public List<OutputGetMemberImage> Images { get; set; } = [];
    public List<OutputGetMemberEvent> Events { get; set; } = [];
    public List<OutputGetMemberSocialLink> SocialLinks { get; set; } = [];
    public List<OutputGetMemberNote> Notes { get; set; } = [];
}

public class OutputGetMemberTree
{
    public long ID_Members { get; set; }
    public long FK_Families { get; set; }
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
    public OutputGetMemberTree? Spouse { get; set; }
    public List<OutputGetMemberTree> Children { get; set; } = [];
    public List<OutputGetMemberImage> Images { get; set; } = [];
    public List<OutputGetMemberSocialLink> SocialLinks { get; set; } = [];
    public List<OutputGetMemberEvent> Events { get; set; } = [];
    public List<OutputGetMemberNote> Notes { get; set; } = [];
}

public class OutputGetMemberAddress
{
    public long ID_MemberAddresses { get; set; }
    public long FK_Members { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; }
}

public class OutputGetMemberImage
{
    public long ID_MemberImages { get; set; }
    public long FK_Members { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class OutputGetMemberEvent
{
    public long ID_MemberEvents { get; set; }
    public long FK_Members { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
}

public class OutputGetMemberNote
{
    public long ID_MemberNotes { get; set; }
    public long FK_Members { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class OutputGetMemberSocialLink
{
    public long ID_MemberSocialLinks { get; set; }
    public long FK_Members { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Username { get; set; }
}
