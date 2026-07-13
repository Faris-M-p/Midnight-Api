using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Models.Entities;

public class Member : AuditableEntity
{
    [Key]
    public long ID_Members { get; set; }

    public long FK_Families { get; set; }
    public long? FK_Members_Parent { get; set; }
    public long? FK_Members_Spouse { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public DateOnly? DateOfDeath { get; set; }
    public bool IsRoot { get; set; }

    [MaxLength(4000)]
    public string? Biography { get; set; }

    [MaxLength(200)]
    public string? Profession { get; set; }

    public Family Family { get; set; } = null!;
    public Member? Parent { get; set; }
    public Member? Spouse { get; set; }
    public ICollection<Member> Children { get; set; } = [];
    public ICollection<MemberAddress> Addresses { get; set; } = [];
    public ICollection<MemberImage> Images { get; set; } = [];
    public ICollection<MemberEvent> Events { get; set; } = [];
    public ICollection<MemberSocialLink> SocialLinks { get; set; } = [];
    public ICollection<MemberNote> Notes { get; set; } = [];
}
