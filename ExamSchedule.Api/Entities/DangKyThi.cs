namespace ExamSchedule.Api.Entities;

public enum TrangThaiDangKyThi
{
    ChoXep,
    DaXep,
    ChuaXep,
    Huy
}

public class DangKyThi
{
    public int DangKyThiId { get; set; }
    public int ThiSinhId { get; set; }
    public ThiSinh ThiSinh { get; set; } = null!;
    public int KyThiId { get; set; }
    public KyThi KyThi { get; set; } = null!;
    public int? CaThiId { get; set; }
    public CaThi? CaThi { get; set; }
    public TrangThaiDangKyThi TrangThai { get; set; } = TrangThaiDangKyThi.ChoXep;
    public string? LyDoChuaXep { get; set; }
    public DateTime NgayDangKy { get; set; } = DateTime.UtcNow;
    public DateTime? NgayCapNhat { get; set; }
}