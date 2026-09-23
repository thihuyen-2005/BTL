namespace ExamSchedule.Api.Entities;

public enum ProctorRole
{
    Truong_ca,
    Giam_thi_1,
    Giam_thi_2
}

public enum ProctorAssignmentStatus
{
    Da_phan_cong,
    Da_huy
}

public class ProctorAssignment
{
    public int ProctorAssignmentId { get; set; }
    public int CaThiId { get; set; }
    public CaThi CaThi { get; set; } = null!;
    public int ProctorProfileId { get; set; }
    public ProctorProfile ProctorProfile { get; set; } = null!;
    public ProctorRole Role { get; set; }
    public ProctorAssignmentStatus Status { get; set; } = ProctorAssignmentStatus.Da_phan_cong;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
}