using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Models.Entities;

public class Family : AuditableEntity
{
    [Key]
    public long ID_Families { get; set; }

    [Required, MaxLength(50)]
    public string FamilyCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FamilyName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public ICollection<Member> Members { get; set; } = [];
    public UserAccount? UserAccount { get; set; }
}
