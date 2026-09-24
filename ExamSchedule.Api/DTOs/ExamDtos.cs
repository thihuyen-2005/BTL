namespace ExamSchedule.Api.DTOs;

public record KyThiCreateDto(
    string MaKyThi, string TenKyThi,
    DateTime ThoiGianBatDauDk, DateTime ThoiGianKetThucDk, string? GhiChu);

public record KyThiUpdateDto(
    string TenKyThi, DateTime ThoiGianKetThucDk, string? GhiChu);

public record KyThiResponseDto(
    int KyThiId, string MaKyThi, string TenKyThi, string LoaiChungChi,
    DateTime ThoiGianBatDauDk, DateTime ThoiGianKetThucDk,
    string TrangThai, int SoCaThi);

public record PagedResult<T>(List<T> Items, int Total, int Page, int Limit);