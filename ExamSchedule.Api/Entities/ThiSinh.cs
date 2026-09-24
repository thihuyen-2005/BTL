namespace ExamSchedule.Api.Entities;

public class ThiSinh
{
    public int ThiSinhId { get; set; }
    public string MaThiSinh { get; set; } = null!;
    public string HoTen { get; set; } = null!;
    public DateTime? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public string? DanToc { get; set; }
    public string? NoiSinh { get; set; }
    public string? QuocTich { get; set; }
    public string? SoCccdHoChieu { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Lop { get; set; }
    public string? NganhHoc { get; set; }
    public string? Khoa { get; set; }
    public decimal? SoTien { get; set; }
    public string? EmailCaNhan { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public ICollection<DangKyThi> DangKyThis { get; set; } = new List<DangKyThi>();
}