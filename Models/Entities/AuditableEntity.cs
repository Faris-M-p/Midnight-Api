namespace MidnightApi.Models.Entities;

public abstract class AuditableEntity
{
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledOn { get; set; }
}
