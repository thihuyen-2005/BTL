namespace ExamSchedule.Api.Entities;

public class ProctorProfile
{
    public int ProctorProfileId { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string StaffCode { get; set; } = null!;
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProctorAssignment> Assignments { get; set; } = new List<ProctorAssignment>();
}