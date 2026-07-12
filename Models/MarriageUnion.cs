using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MidnightApi.Models;

public class MarriageUnion
{
    [Key]
    [MaxLength(120)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Spouse1Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Spouse2Id { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public List<string> ChildrenIds { get; set; } = [];
}
