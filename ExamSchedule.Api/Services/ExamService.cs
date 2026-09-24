using ExamSchedule.Api.Data;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Entities;
using ExamSchedule.Api.Middlewares;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Services;

public class ExamService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly ExamStatusUpdater _statusUpdater;              // ← THÊM 1

    public ExamService(AppDbContext db, AuditService audit, 
                       ExamStatusUpdater statusUpdater)             // ← THÊM 2
    {
        _db = db;
        _audit = audit;
        _statusUpdater = statusUpdater;                             // ← THÊM 3
    }

    public async Task<PagedResult<KyThiResponseDto>> GetPagedAsync(int page, int limit, string? status)
    {
        await _statusUpdater.UpdateAllAsync(); 
        var q = _db.KyThis.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status) && 
    Enum.TryParse<TrangThaiKyThi>(status, out var tt))
            {
                q = q.Where(k => k.TrangThai == tt);
            }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(k => k.KyThiId)
            .Skip((page - 1) * limit).Take(limit)
            .Select(k => new KyThiResponseDto(
                k.KyThiId, k.MaKyThi, $"Kỳ thi Năng lực số - {k.MaKyThi}", "NangLucSo",
                k.ThoiGianBatDauDk, k.ThoiGianKetThucDk,
                k.TrangThai.ToString(),
                k.CaThis.Count))
            .ToListAsync();

        return new PagedResult<KyThiResponseDto>(items, total, page, limit);
    }

    public async Task<KyThiResponseDto> GetByIdAsync(int id)
    {
        await _statusUpdater.UpdateAllAsync(); 
        var k = await _db.KyThis.Include(x => x.CaThis)
            .FirstOrDefaultAsync(x => x.KyThiId == id)
            ?? throw new NotFoundException("Không tìm thấy kỳ thi.");

        return new KyThiResponseDto(
            k.KyThiId, k.MaKyThi, TenKyThiChuan(k), "NangLucSo",
            k.ThoiGianBatDauDk, k.ThoiGianKetThucDk,
            k.TrangThai.ToString(), k.CaThis.Count);
    }

    public async Task<KyThiResponseDto> CreateAsync(KyThiCreateDto dto, int userId, string ip)
    {
        if (dto.ThoiGianKetThucDk < dto.ThoiGianBatDauDk)
            throw new BusinessException("Ngày kết thúc ĐK phải >= ngày bắt đầu.");

        if (await _db.KyThis.AnyAsync(k => k.MaKyThi == dto.MaKyThi))
            throw new BusinessException("Mã kỳ thi đã tồn tại.");

        if (!string.Equals(dto.LoaiChungChi, "NangLucSo", StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Hệ thống chỉ hỗ trợ kỳ thi Năng lực số.");

        var kt = new KyThi
        {
            MaKyThi = dto.MaKyThi,
            TenKyThi = dto.TenKyThi,
            LoaiChungChi = "NangLucSo",
            ThoiGianBatDauDk = dto.ThoiGianBatDauDk,
            ThoiGianKetThucDk = dto.ThoiGianKetThucDk,
            GhiChu = dto.GhiChu,
            TrangThai = TrangThaiKyThi.MoiTao
        };
        _db.KyThis.Add(kt);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "CREATE", "KY_THI", kt.KyThiId, null, kt, ip);

        return new KyThiResponseDto(kt.KyThiId, kt.MaKyThi, TenKyThiChuan(kt),
            "NangLucSo", kt.ThoiGianBatDauDk, kt.ThoiGianKetThucDk,
            kt.TrangThai.ToString(), 0);
    }

    private static string TenKyThiChuan(KyThi kyThi) => $"Kỳ thi Năng lực số - {kyThi.MaKyThi}";

    public async Task UpdateAsync(int id, KyThiUpdateDto dto, int userId, string ip)
    {
        var kt = await _db.KyThis.FindAsync(id)
            ?? throw new NotFoundException("Không tìm thấy kỳ thi.");

        var old = new { kt.TenKyThi, kt.ThoiGianKetThucDk, kt.GhiChu };

        kt.TenKyThi = dto.TenKyThi;
        kt.ThoiGianKetThucDk = dto.ThoiGianKetThucDk;
        kt.GhiChu = dto.GhiChu;
        kt.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "UPDATE", "KY_THI", id, old,
            new { kt.TenKyThi, kt.ThoiGianKetThucDk, kt.GhiChu }, ip);
    }

    public async Task DeleteAsync(int id, int userId, string ip)
    {
        var kt = await _db.KyThis.Include(k => k.CaThis)
            .FirstOrDefaultAsync(k => k.KyThiId == id)
            ?? throw new NotFoundException("Không tìm thấy kỳ thi.");

        if (kt.CaThis.Any())
            throw new BusinessException("Không thể xóa: kỳ thi đã có ca thi.");

        _db.KyThis.Remove(kt);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "DELETE", "KY_THI", id, kt, null, ip);
    }
}