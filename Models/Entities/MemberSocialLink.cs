using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Models.Entities;

public class MemberSocialLink : AuditableEntity
{
    [Key]
    public long ID_MemberSocialLinks { get; set; }

    public long FK_Members { get; set; }

    [Required, MaxLength(50)]
    public string Platform { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Username { get; set; }

    public Member Member { get; set; } = null!;
}
