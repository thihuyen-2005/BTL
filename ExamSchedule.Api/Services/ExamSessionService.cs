using ExamSchedule.Api.Data;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Entities;
using ExamSchedule.Api.Middlewares;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Services;

public class ExamSessionService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly ExamStatusUpdater _statusUpdater;

    // Thời lượng cố định mỗi ca thi Năng lực số
    private const int DURATION_MINUTES = 120;

    public ExamSessionService(AppDbContext db, AuditService audit,
                              ExamStatusUpdater statusUpdater)
    {
        _db = db;
        _audit = audit;
        _statusUpdater = statusUpdater;
    }

    public async Task<List<CaThiResponseDto>> GetByKyThiAsync(
        int kyThiId, string? trangThai, string? keyword)
    {
        var q = _db.CaThis.Where(c => c.KyThiId == kyThiId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            q = q.Where(c => c.CaThiId.ToString().Contains(keyword)
                || c.KyThi.MaKyThi.Contains(keyword)
                || c.KyThi.TenKyThi.Contains(keyword)
                || c.PhongThi.MaPhong.Contains(keyword)
                || c.PhongThi.TenPhong.Contains(keyword)
                || (c.GhiChu != null && c.GhiChu.Contains(keyword)));
        }

        if (!string.IsNullOrEmpty(trangThai) &&
            Enum.TryParse<TrangThaiCaThi>(trangThai, out var tt))
        {
            q = tt == TrangThaiCaThi.Du_kien
                ? q.Where(c => c.TrangThai == TrangThaiCaThi.Du_kien || c.TrangThai == TrangThaiCaThi.Cho_xep)
                : q.Where(c => c.TrangThai == tt);
        }

        var sessions = await q.OrderBy(c => c.ThoiGianBatDau)
            .Select(c => new
            {
                c.CaThiId,
                c.KyThiId,
                MaKyThi = c.KyThi.MaKyThi,
                TenKyThi = c.KyThi.TenKyThi,
                c.PhongThiId,
                MaPhong = c.PhongThi.MaPhong,
                TenPhong = c.PhongThi.TenPhong,
                c.ThoiGianBatDau,
                c.ThoiGianKetThuc,
                c.SucChua,
                DaXep = c.DangKyThis.Count(registration =>
                    registration.TrangThai == TrangThaiDangKyThi.DaXep),
                ChuaXep = _db.DangKyThis.Count(registration =>
                    registration.KyThiId == c.KyThiId
                    && registration.CaThiId == null
                    && registration.TrangThai != TrangThaiDangKyThi.Huy),
                c.TrangThai,
                c.GhiChu,
                AssignedProctorCount = c.ProctorAssignments.Count(a =>
                    a.Status == ProctorAssignmentStatus.Da_phan_cong)
            })
            .ToListAsync();

        return sessions.Select(c => new CaThiResponseDto(
            c.CaThiId, c.KyThiId, c.MaKyThi, c.TenKyThi,
            c.PhongThiId, c.MaPhong, c.TenPhong,
            c.ThoiGianBatDau, c.ThoiGianKetThuc, c.SucChua,
            c.DaXep, Math.Max(0, c.SucChua - c.DaXep), c.ChuaXep,
            c.TrangThai.ToString(), c.GhiChu, c.AssignedProctorCount)).ToList();
    }

    public async Task<CaThiResponseDto> CreateAsync(CaThiCreateDto dto, int userId, string ip)
    {
        // Tự tính thời gian kết thúc = bắt đầu + 120 phút
        var thoiGianBatDau = dto.ThoiGianBatDau;
        var thoiGianKetThuc = thoiGianBatDau.AddMinutes(DURATION_MINUTES);

        ValidateTime(thoiGianBatDau, thoiGianKetThuc, dto.SucChua);

        var kt = await _db.KyThis.FindAsync(dto.KyThiId)
            ?? throw new NotFoundException("Kỳ thi không tồn tại.");
        var room = await _db.PhongThis.FindAsync(dto.PhongThiId)
            ?? throw new NotFoundException("Phòng thi không tồn tại.");
        if (dto.SucChua > room.SucChua)
            throw new BusinessException($"Sức chứa ca thi không được vượt quá sức chứa phòng ({room.SucChua}).");

        if (kt.TrangThai is TrangThaiKyThi.KetThuc or TrangThaiKyThi.Huy)
            throw new BusinessException("Kỳ thi đã kết thúc/hủy, không thể tạo ca thi.");

        await EnsureNoConflictAsync(dto.PhongThiId,
            thoiGianBatDau, thoiGianKetThuc, ignoreId: 0);

        var ca = new CaThi
        {
            KyThiId = dto.KyThiId,
            PhongThiId = dto.PhongThiId,
            ThoiGianBatDau = thoiGianBatDau,
            ThoiGianKetThuc = thoiGianKetThuc,      // tự tính
            SucChua = dto.SucChua,
            GhiChu = dto.GhiChu,
            TrangThai = TrangThaiCaThi.Du_kien
        };
        _db.CaThis.Add(ca);
        await _db.SaveChangesAsync();

        await _db.Entry(ca).Reference(c => c.KyThi).LoadAsync();
        await _db.Entry(ca).Reference(c => c.PhongThi).LoadAsync();

        await _audit.LogAsync(userId, "CREATE", "CA_THI", ca.CaThiId, null, ca, ip);
        await _statusUpdater.UpdateAllAsync();

        return ToDto(ca, 0, 0);
    }

    public async Task UpdateAsync(int id, CaThiUpdateDto dto, int userId, string ip)
    {
        var ca = await _db.CaThis.Include(c => c.KyThi).Include(c => c.PhongThi)
            .FirstOrDefaultAsync(c => c.CaThiId == id)
            ?? throw new NotFoundException("Không tìm thấy ca thi.");

        if (ca.TrangThai is TrangThaiCaThi.Dong or TrangThaiCaThi.Huy)
            throw new BusinessException("Ca thi đã đóng/hủy, không thể sửa.");

        // Tự tính thời gian kết thúc
        var thoiGianBatDau = dto.ThoiGianBatDau;
        var thoiGianKetThuc = thoiGianBatDau.AddMinutes(DURATION_MINUTES);

        ValidateTime(thoiGianBatDau, thoiGianKetThuc, dto.SucChua);

        var room = await _db.PhongThis.FindAsync(dto.PhongThiId)
            ?? throw new NotFoundException("Phòng thi không tồn tại.");
        if (dto.SucChua > room.SucChua)
            throw new BusinessException($"Sức chứa ca thi không được vượt quá sức chứa phòng ({room.SucChua}).");

        await EnsureNoConflictAsync(dto.PhongThiId,
            thoiGianBatDau, thoiGianKetThuc, ignoreId: id);

        var old = new { ca.PhongThiId, ca.ThoiGianBatDau, ca.ThoiGianKetThuc, ca.SucChua };
        ca.PhongThiId = dto.PhongThiId;
        ca.ThoiGianBatDau = thoiGianBatDau;
        ca.ThoiGianKetThuc = thoiGianKetThuc;
        ca.SucChua = dto.SucChua;
        ca.GhiChu = dto.GhiChu;
        ca.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "UPDATE", "CA_THI", id, old,
            new { ca.PhongThiId, ca.ThoiGianBatDau, ca.ThoiGianKetThuc, ca.SucChua }, ip);
        await _statusUpdater.UpdateAllAsync();
    }

    public async Task CloseAsync(int id, int userId, string ip)
    {
        var ca = await _db.CaThis.FindAsync(id)
            ?? throw new NotFoundException("Không tìm thấy ca thi.");

        if (ca.TrangThai == TrangThaiCaThi.Dong)
            throw new BusinessException("Ca thi đã đóng.");
        if (ca.TrangThai != TrangThaiCaThi.Da_xep)
            throw new BusinessException("Chỉ đóng ca thi khi đã xếp lịch xong.");

        ca.TrangThai = TrangThaiCaThi.Dong;
        ca.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "CLOSE", "CA_THI", id, null, new { TrangThai = "Dong" }, ip);
        await _statusUpdater.UpdateAllAsync();
    }

    public async Task CancelAsync(int id, int userId, string ip)
    {
        var ca = await _db.CaThis.FindAsync(id)
            ?? throw new NotFoundException("Không tìm thấy ca thi.");

        if (ca.TrangThai == TrangThaiCaThi.Dong)
            throw new BusinessException("Ca thi đã đóng, không thể hủy.");
        if (ca.TrangThai == TrangThaiCaThi.Huy)
            throw new BusinessException("Ca thi đã bị hủy, không thể hủy lại.");

        ca.TrangThai = TrangThaiCaThi.Huy;
        ca.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "CANCEL", "CA_THI", id, null, new { TrangThai = "Huy" }, ip);
        await _statusUpdater.UpdateAllAsync();
    }

    public async Task DeleteAsync(int id, int userId, string ip)
    {
        var ca = await _db.CaThis
            .Include(c => c.ProctorAssignments)
            .Include(c => c.DangKyThis)
            .FirstOrDefaultAsync(c => c.CaThiId == id)
            ?? throw new NotFoundException("Không tìm thấy ca thi.");

        if (ca.ProctorAssignments.Count > 0)
            _db.ProctorAssignments.RemoveRange(ca.ProctorAssignments);
        if (ca.DangKyThis.Count > 0)
            _db.DangKyThis.RemoveRange(ca.DangKyThis);

        _db.CaThis.Remove(ca);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "DELETE", "CA_THI", id, ca, null, ip);
        await _statusUpdater.UpdateAllAsync();
    }

    // ====== LOGIC KIỂM TRA XUNG ĐỘT (FR-EXAM-03) ======
    private async Task EnsureNoConflictAsync(int phongThiId, DateTime start, DateTime end, int ignoreId)
    {
        var conflict = await _db.CaThis
            .Where(c => c.CaThiId != ignoreId
                     && c.PhongThiId == phongThiId
                     && c.TrangThai != TrangThaiCaThi.Huy
                     && start < c.ThoiGianKetThuc
                     && end > c.ThoiGianBatDau)
            .Include(c => c.PhongThi)
            .FirstOrDefaultAsync();

        if (conflict != null)
            throw new ConflictException(
                $"Phòng {conflict.PhongThi.MaPhong} đã có ca thi từ " +
                $"{conflict.ThoiGianBatDau:HH:mm dd/MM/yyyy} " +
                $"đến {conflict.ThoiGianKetThuc:HH:mm dd/MM/yyyy}.");
    }

    private static void ValidateTime(DateTime start, DateTime end, int sucChua)
    {
        if (end != start.AddMinutes(DURATION_MINUTES))
            throw new BusinessException($"Mỗi ca thi Năng lực số trên máy phải kéo dài đúng {DURATION_MINUTES} phút.");
        if (sucChua <= 0)
            throw new BusinessException("Sức chứa phải lớn hơn 0.");
    }

    private static CaThiResponseDto ToDto(CaThi c, int daXep, int chuaXep) {
        var conLai = Math.Max(0, c.SucChua - daXep);
        return new CaThiResponseDto(
            c.CaThiId, c.KyThiId, c.KyThi?.MaKyThi ?? "", c.KyThi?.TenKyThi ?? "",
            c.PhongThiId, c.PhongThi?.MaPhong ?? "", c.PhongThi?.TenPhong ?? "",
            c.ThoiGianBatDau, c.ThoiGianKetThuc, c.SucChua,
            daXep, conLai, chuaXep,
            c.TrangThai.ToString(), c.GhiChu, 0);
    }
}