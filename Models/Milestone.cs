using System.ComponentModel.DataAnnotations;

namespace MidnightApi.Models;

public class Milestone
{
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    public int Year { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string MemberId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string MemberName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
}
