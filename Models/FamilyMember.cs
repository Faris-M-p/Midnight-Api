using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MidnightApi.Models;

public class FamilyMember
{
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Relation { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    public DateOnly Dob { get; set; }

    [Required]
    [MaxLength(120)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Profession { get; set; } = string.Empty;

    [Required]
    public string Avatar { get; set; } = string.Empty;

    [Required]
    public string Bio { get; set; } = string.Empty;

    [Required]
    public string Education { get; set; } = string.Empty;

    [Required]
    public string Career { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public List<string> Photos { get; set; } = [];

    public bool IsDeceased { get; set; }

    [Column(TypeName = "jsonb")]
    public Dictionary<string, string>? Socials { get; set; }
}
