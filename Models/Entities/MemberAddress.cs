using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Models.Entities;

public class MemberAddress : AuditableEntity
{
    [Key]
    public long ID_MemberAddresses { get; set; }

    public long FK_Members { get; set; }

    [Required, MaxLength(300)]
    public string AddressLine1 { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? AddressLine2 { get; set; }

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? State { get; set; }

    [Required, MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    public bool IsPrimary { get; set; }
    public Member Member { get; set; } = null!;
}
