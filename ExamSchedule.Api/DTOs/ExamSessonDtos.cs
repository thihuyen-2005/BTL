namespace ExamSchedule.Api.DTOs;

public record CaThiCreateDto(
    int KyThiId, int PhongThiId,
    DateTime ThoiGianBatDau,
    int SucChua, string? GhiChu);

public record CaThiUpdateDto(
    int PhongThiId, DateTime ThoiGianBatDau,
    int SucChua, string? GhiChu);

public record CaThiResponseDto(
    int CaThiId, int KyThiId, string MaKyThi, string TenKyThi,
    int PhongThiId, string MaPhong, string TenPhong,
    DateTime ThoiGianBatDau, DateTime ThoiGianKetThuc,
    int SucChua, int DaXep, int ConLai, int ChuaXep,
    string TrangThai, string? GhiChu);