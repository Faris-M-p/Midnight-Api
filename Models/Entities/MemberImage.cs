using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Models.Entities;

public class MemberImage : AuditableEntity
{
    [Key]
    public long ID_MemberImages { get; set; }

    public long FK_Members { get; set; }

    [Required, MaxLength(2000)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Caption { get; set; }

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public Member Member { get; set; } = null!;
}
