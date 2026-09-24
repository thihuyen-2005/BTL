namespace ExamSchedule.Api.DTOs;

public record ThiSinhImportDto(
    string MaThiSinh, string HoTen, DateTime? NgaySinh, string? GioiTinh,
    string? DanToc, string? NoiSinh, string? QuocTich, string? SoCccdHoChieu,
    string? SoDienThoai, string? Lop, string? NganhHoc, string? Khoa,
    decimal? SoTien, string? EmailCaNhan);

public record ThiSinhCreateDto(
    string MaThiSinh, string HoTen, DateTime? NgaySinh, string? GioiTinh,
    string? DanToc, string? NoiSinh, string? QuocTich, string? SoCccdHoChieu,
    string? SoDienThoai, string? Lop, string? NganhHoc, string? Khoa,
    decimal? SoTien, string? EmailCaNhan) : ThiSinhImportDto(
        MaThiSinh, HoTen, NgaySinh, GioiTinh, DanToc, NoiSinh, QuocTich,
        SoCccdHoChieu, SoDienThoai, Lop, NganhHoc, Khoa, SoTien, EmailCaNhan);

public record ThiSinhUpdateDto(
    string MaThiSinh, string HoTen, DateTime? NgaySinh, string? GioiTinh,
    string? DanToc, string? NoiSinh, string? QuocTich, string? SoCccdHoChieu,
    string? SoDienThoai, string? Lop, string? NganhHoc, string? Khoa,
    decimal? SoTien, string? EmailCaNhan) : ThiSinhImportDto(
        MaThiSinh, HoTen, NgaySinh, GioiTinh, DanToc, NoiSinh, QuocTich,
        SoCccdHoChieu, SoDienThoai, Lop, NganhHoc, Khoa, SoTien, EmailCaNhan);

public record ThiSinhImportResultDto(int SoLuong, List<string> Loi);

public record ThiSinhResponseDto(
    int ThiSinhId, string MaThiSinh, string HoTen, DateTime? NgaySinh,
    string? GioiTinh, string? DanToc, string? NoiSinh, string? QuocTich,
    string? SoCccdHoChieu, string? SoDienThoai, string? Lop, string? NganhHoc,
    string? Khoa, decimal? SoTien, string? EmailCaNhan);

public record DangKyThiCreateDto(string MaThiSinh, int KyThiId);

public record DangKyThiResponseDto(
    int DangKyThiId, int ThiSinhId, string MaThiSinh, string HoTen,
    int KyThiId, int? CaThiId, string TrangThai, string? LyDoChuaXep,
    DateTime? ThoiGianBatDau, DateTime? ThoiGianKetThuc, string? MaPhong);

public record XepLichResponseDto(
    int KyThiId, int TongSoDangKy, int DaXep, int ChuaXep,
    List<DangKyThiResponseDto> ChiTiet);