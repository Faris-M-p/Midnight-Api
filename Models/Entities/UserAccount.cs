using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Models.Entities;

public class UserAccount : AuditableEntity
{
    [Key]
    public long ID_UserAccounts { get; set; }

    public long FK_Members { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public Member Member { get; set; } = null!;
}
