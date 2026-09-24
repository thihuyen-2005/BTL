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

    public ExamSessionService(AppDbContext db, AuditService audit,
                              ExamStatusUpdater statusUpdater)
    {
        _db = db;
        _audit = audit;
        _statusUpdater = statusUpdater;
    }

    public async Task<List<CaThiResponseDto>> GetByKyThiAsync(int kyThiId, string? trangThai)
    {
        var q = _db.CaThis
            .Include(c => c.KyThi).Include(c => c.PhongThi)
            .Where(c => c.KyThiId == kyThiId);

        if (!string.IsNullOrEmpty(trangThai) &&
            Enum.TryParse<TrangThaiCaThi>(trangThai, out var tt))
        {
            q = q.Where(c => c.TrangThai == tt);
        }

        return await q.OrderBy(c => c.ThoiGianBatDau)
            .Select(c => ToDto(c)).ToListAsync();
    }

    public async Task<CaThiResponseDto> CreateAsync(CaThiCreateDto dto, int userId, string ip)
    {
        ValidateTime(dto.ThoiGianBatDau, dto.ThoiGianKetThuc, dto.SucChua);

        var kt = await _db.KyThis.FindAsync(dto.KyThiId)
            ?? throw new NotFoundException("Kỳ thi không tồn tại.");
        var room = await _db.PhongThis.FindAsync(dto.PhongThiId)
            ?? throw new NotFoundException("Phòng thi không tồn tại.");
        if (dto.SucChua > room.SucChua)
            throw new BusinessException($"Sức chứa ca thi không được vượt quá sức chứa phòng ({room.SucChua}).");

        if (kt.TrangThai is TrangThaiKyThi.KetThuc or TrangThaiKyThi.Huy)
            throw new BusinessException("Kỳ thi đã kết thúc/hủy, không thể tạo ca thi.");

        await EnsureNoConflictAsync(dto.PhongThiId,
            dto.ThoiGianBatDau, dto.ThoiGianKetThuc, ignoreId: 0);

        var ca = new CaThi
        {
            KyThiId = dto.KyThiId,
            PhongThiId = dto.PhongThiId,
            ThoiGianBatDau = dto.ThoiGianBatDau,
            ThoiGianKetThuc = dto.ThoiGianKetThuc,
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

        return ToDto(ca);
    }

    public async Task UpdateAsync(int id, CaThiUpdateDto dto, int userId, string ip)
    {
        var ca = await _db.CaThis.Include(c => c.KyThi).Include(c => c.PhongThi)
            .FirstOrDefaultAsync(c => c.CaThiId == id)
            ?? throw new NotFoundException("Không tìm thấy ca thi.");

        if (ca.TrangThai is TrangThaiCaThi.Dong or TrangThaiCaThi.Huy)
            throw new BusinessException("Ca thi đã đóng/hủy, không thể sửa.");

        ValidateTime(dto.ThoiGianBatDau, dto.ThoiGianKetThuc, dto.SucChua);
        var room = await _db.PhongThis.FindAsync(dto.PhongThiId)
            ?? throw new NotFoundException("Phòng thi không tồn tại.");
        if (dto.SucChua > room.SucChua)
            throw new BusinessException($"Sức chứa ca thi không được vượt quá sức chứa phòng ({room.SucChua}).");
        await EnsureNoConflictAsync(dto.PhongThiId,
            dto.ThoiGianBatDau, dto.ThoiGianKetThuc, ignoreId: id);

        var old = new { ca.PhongThiId, ca.ThoiGianBatDau, ca.ThoiGianKetThuc, ca.SucChua };
        ca.PhongThiId = dto.PhongThiId;
        ca.ThoiGianBatDau = dto.ThoiGianBatDau;
        ca.ThoiGianKetThuc = dto.ThoiGianKetThuc;
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

        ca.TrangThai = TrangThaiCaThi.Huy;
        ca.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "CANCEL", "CA_THI", id, null, new { TrangThai = "Huy" }, ip);
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
        if (end != start.AddMinutes(120))
            throw new BusinessException("Mỗi ca thi Năng lực số trên máy phải kéo dài đúng 120 phút.");
        if (sucChua <= 0)
            throw new BusinessException("Sức chứa phải lớn hơn 0.");
    }

    private static CaThiResponseDto ToDto(CaThi c) =>
        new(c.CaThiId, c.KyThiId, c.KyThi?.MaKyThi ?? "", c.KyThi?.TenKyThi ?? "",
            c.PhongThiId, c.PhongThi?.MaPhong ?? "", c.PhongThi?.TenPhong ?? "",
            c.ThoiGianBatDau, c.ThoiGianKetThuc, c.SucChua,
            c.TrangThai.ToString(), c.GhiChu);
}