using ExamSchedule.Api.Data;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Entities;
using ExamSchedule.Api.Middlewares;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Services;

public class XepLichService
{
    private readonly AppDbContext _db;

    public XepLichService(AppDbContext db) => _db = db;

    public async Task<XepLichResponseDto> XepLichAsync(int kyThiId)
    {
        var exam = await _db.KyThis.FindAsync(kyThiId)
            ?? throw new NotFoundException("Không tìm thấy kỳ thi.");
        if (exam.TrangThai is TrangThaiKyThi.KetThuc or TrangThaiKyThi.Huy)
            throw new BusinessException("Kỳ thi đã kết thúc hoặc bị hủy.");

        var registrations = await _db.DangKyThis
            .Include(x => x.ThiSinh)
            .Include(x => x.CaThi).ThenInclude(x => x!.PhongThi)
            .Where(x => x.KyThiId == kyThiId && x.TrangThai != TrangThaiDangKyThi.Huy)
            .OrderBy(x => x.DangKyThiId)
            .ToListAsync();
        var sessions = await _db.CaThis.Include(x => x.PhongThi)
            .Where(x => x.KyThiId == kyThiId && x.TrangThai != TrangThaiCaThi.Dong && x.TrangThai != TrangThaiCaThi.Huy)
            .OrderBy(x => x.ThoiGianBatDau).ToListAsync();

        foreach (var registration in registrations.Where(x => x.ThiSinh.SoTien != 800000m))
        {
            registration.CaThiId = null;
            registration.CaThi = null;
            registration.TrangThai = TrangThaiDangKyThi.ChuaXep;
            registration.LyDoChuaXep = "Chưa nộp đủ lệ phí 800.000 đồng, không được xếp ca thi.";
            registration.NgayCapNhat = DateTime.UtcNow;
        }

        var occupied = await _db.DangKyThis.Where(x => x.CaThiId.HasValue && x.TrangThai == TrangThaiDangKyThi.DaXep)
            .GroupBy(x => x.CaThiId!.Value).Select(x => new { CaThiId = x.Key, SoLuong = x.Count() })
            .ToDictionaryAsync(x => x.CaThiId, x => x.SoLuong);
        var existingSchedules = await _db.DangKyThis.Include(x => x.CaThi)
            .Where(x => x.TrangThai == TrangThaiDangKyThi.DaXep && x.CaThiId.HasValue)
            .ToListAsync();

        foreach (var registration in registrations.Where(x => x.ThiSinh.SoTien == 800000m && x.CaThiId == null && x.TrangThai != TrangThaiDangKyThi.DaXep))
        {
            var selected = sessions.FirstOrDefault(session =>
                occupied.GetValueOrDefault(session.CaThiId) < session.SucChua &&
                !existingSchedules.Any(old => old.ThiSinhId == registration.ThiSinhId && old.CaThi != null &&
                    session.ThoiGianBatDau < old.CaThi.ThoiGianKetThuc && session.ThoiGianKetThuc > old.CaThi.ThoiGianBatDau));

            if (selected == null)
            {
                registration.TrangThai = TrangThaiDangKyThi.ChuaXep;
                registration.LyDoChuaXep = sessions.Count == 0
                    ? "Không có ca thi khả dụng."
                    : "Không còn sức chứa hoặc bị trùng giờ thi.";
                registration.NgayCapNhat = DateTime.UtcNow;
                continue;
            }

            registration.CaThiId = selected.CaThiId;
            registration.CaThi = selected;
            registration.TrangThai = TrangThaiDangKyThi.DaXep;
            registration.LyDoChuaXep = null;
            registration.NgayCapNhat = DateTime.UtcNow;
            occupied[selected.CaThiId] = occupied.GetValueOrDefault(selected.CaThiId) + 1;
            existingSchedules.Add(registration);
            selected.TrangThai = TrangThaiCaThi.Da_xep;
        }

        exam.TrangThai = TrangThaiKyThi.DangLapLich;
        exam.NgayCapNhat = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new XepLichResponseDto(kyThiId, registrations.Count,
            registrations.Count(x => x.TrangThai == TrangThaiDangKyThi.DaXep),
            registrations.Count(x => x.TrangThai == TrangThaiDangKyThi.ChuaXep),
            registrations.Select(ThiSinhService.ToRegistrationDto).ToList());
    }
}