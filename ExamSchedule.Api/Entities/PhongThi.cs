namespace ExamSchedule.Api.Entities;

public class PhongThi
{
    public int PhongThiId { get; set; }
    public string MaPhong { get; set; } = null!;
    public string TenPhong { get; set; } = null!;
    public int SucChua { get; set; }
    public string? ViTri { get; set; }
}