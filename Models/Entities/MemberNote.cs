using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Models.Entities;

public class MemberNote : AuditableEntity
{
    [Key]
    public long ID_MemberNotes { get; set; }

    public long FK_Members { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public Member Member { get; set; } = null!;
}
