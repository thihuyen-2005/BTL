namespace ExamSchedule.Api.Entities;

public class AuditLog
{
    public long AuditId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = null!;
    public string Entity { get; set; } = null!;
    public int EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}