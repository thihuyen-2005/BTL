using ExamSchedule.Api.Data;
using ExamSchedule.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Services;

public class ExamStatusUpdater
{
    private readonly AppDbContext _db;
    private static readonly TimeZoneInfo ExamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public ExamStatusUpdater(AppDbContext db) { _db = db; }

    /// Cập nhật trạng thái kỳ thi và ca thi dựa trên lịch phân công và thời gian hiện tại.
    public async Task UpdateAllAsync()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ExamTimeZone);
        var updatedAt = DateTime.UtcNow;

        var exams = await _db.KyThis
            .Include(k => k.CaThis).ThenInclude(c => c.ProctorAssignments)
            .Where(k => k.TrangThai != TrangThaiKyThi.Huy)  // Bỏ qua đã hủy
            .ToListAsync();

        bool changed = false;
        foreach (var kt in exams)
        {
            var caThis = kt.CaThis
                .Where(c => c.TrangThai != TrangThaiCaThi.Huy)
                .ToList();
            var examCloseAt = caThis.Count == 0
                ? (DateTime?)null
                : caThis.Max(c => c.ThoiGianKetThuc).Date.AddDays(1);

            var newStatus = Calculate(caThis, now);
            if (newStatus != kt.TrangThai)
            {
                kt.TrangThai = newStatus;
                kt.NgayCapNhat = updatedAt;
                changed = true;
            }

            foreach (var session in kt.CaThis)
            {
                if (session.TrangThai is TrangThaiCaThi.Dong or TrangThaiCaThi.Huy)
                    continue;

                var assignedCount = session.ProctorAssignments.Count(a =>
                    a.Status == ProctorAssignmentStatus.Da_phan_cong);
                var sessionStatus = examCloseAt.HasValue && now >= examCloseAt.Value
                    ? TrangThaiCaThi.Dong
                    : assignedCount > 0
                        ? TrangThaiCaThi.Da_xep
                        : TrangThaiCaThi.Du_kien;

                if (sessionStatus != session.TrangThai)
                {
                    session.TrangThai = sessionStatus;
                    session.NgayCapNhat = updatedAt;
                    changed = true;
                }
            }
        }
        if (changed) await _db.SaveChangesAsync();
    }

    /// Tính trạng thái dựa trên ca thi
    private static TrangThaiKyThi Calculate(List<CaThi> caThis, DateTime now)
    {
        // Không có ca thi → Mới tạo
        if (caThis.Count == 0) return TrangThaiKyThi.MoiTao;

        var minStart = caThis.Min(c => c.ThoiGianBatDau);
        var closeAt = caThis.Max(c => c.ThoiGianKetThuc).Date.AddDays(1);

        if (now < minStart) return TrangThaiKyThi.DangLapLich;   // Chưa bắt đầu
        if (now < closeAt)  return TrangThaiKyThi.DangThi;        // Đang thi
        return TrangThaiKyThi.KetThuc;                            // Đã xong
    }
}