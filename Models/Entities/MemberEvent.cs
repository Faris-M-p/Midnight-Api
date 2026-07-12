using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Models.Entities;

public class MemberEvent : AuditableEntity
{
    [Key]
    public long ID_MemberEvents { get; set; }

    public long FK_Members { get; set; }

    [Required, MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateOnly EventDate { get; set; }
    public Member Member { get; set; } = null!;
}
