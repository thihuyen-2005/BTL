namespace ExamSchedule.Api.Entities;

public enum TrangThaiCaThi
{
    Du_kien, Cho_xep, Da_xep, Dong, Huy
}

public class CaThi
{
    public int CaThiId { get; set; }
    public int KyThiId { get; set; }
    public KyThi KyThi { get; set; } = null!;

    public int PhongThiId { get; set; }
    public PhongThi PhongThi { get; set; } = null!;

    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianKetThuc { get; set; }
    public int SucChua { get; set; }
    public TrangThaiCaThi TrangThai { get; set; } = TrangThaiCaThi.Du_kien;
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayCapNhat { get; set; }
    public int RequiredProctorCount { get; set; } = 3;
    public string HinhThucThi { get; set; } = "TrenMay";
    public ICollection<ProctorAssignment> ProctorAssignments { get; set; } = new List<ProctorAssignment>();
    public ICollection<DangKyThi> DangKyThis { get; set; } = new List<DangKyThi>();
}