using ExamSchedule.Api.Data;
using ExamSchedule.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Services;

public class ExamStatusUpdater
{
    private readonly AppDbContext _db;
    public ExamStatusUpdater(AppDbContext db) { _db = db; }

    /// Cập nhật trạng thái tất cả kỳ thi dựa trên ca thi + thời gian hiện tại
    public async Task UpdateAllAsync()
    {
        var now = DateTime.UtcNow;

        var exams = await _db.KyThis
            .Include(k => k.CaThis)
            .Where(k => k.TrangThai != TrangThaiKyThi.Huy)  // Bỏ qua đã hủy
            .ToListAsync();

        bool changed = false;
        foreach (var kt in exams)
        {
            var newStatus = Calculate(kt, now);
            if (newStatus != kt.TrangThai)
            {
                kt.TrangThai = newStatus;
                kt.NgayCapNhat = now;
                changed = true;
            }
        }
        if (changed) await _db.SaveChangesAsync();
    }

    /// Tính trạng thái dựa trên ca thi
    private static TrangThaiKyThi Calculate(KyThi kt, DateTime now)
    {
        // Lọc ca thi chưa bị hủy
        var caThis = kt.CaThis
            .Where(c => c.TrangThai != TrangThaiCaThi.Huy)
            .ToList();

        // Không có ca thi → Mới tạo
        if (caThis.Count == 0) return TrangThaiKyThi.MoiTao;

        var minStart = caThis.Min(c => c.ThoiGianBatDau);
        var maxEnd   = caThis.Max(c => c.ThoiGianKetThuc);

        if (now < minStart) return TrangThaiKyThi.DangLapLich;   // Chưa bắt đầu
        if (now <= maxEnd)  return TrangThaiKyThi.DangThi;        // Đang thi
        return TrangThaiKyThi.KetThuc;                            // Đã xong
    }
}