namespace ExamSchedule.Api.Entities;

public enum TrangThaiKyThi
{
    MoiTao, DangLapLich, DangThi, KetThuc, Huy
}

public class KyThi
{
    public int KyThiId { get; set; }
    public string MaKyThi { get; set; } = null!;
    public string TenKyThi { get; set; } = null!;
    public string LoaiChungChi { get; set; } = null!;
    public DateTime ThoiGianBatDauDk { get; set; }
    public DateTime ThoiGianKetThucDk { get; set; }
    public TrangThaiKyThi TrangThai { get; set; } = TrangThaiKyThi.MoiTao;
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayCapNhat { get; set; }

    public ICollection<CaThi> CaThis { get; set; } = new List<CaThi>();
}