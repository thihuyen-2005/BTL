using ExamSchedule.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "CanViewExam")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetOverview([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (to <= from)
            return BadRequest("Khoảng thời gian không hợp lệ.");

        var userCount = await _db.Users.CountAsync();
        var sessions = await _db.CaThis.AsNoTracking()
            .Where(session => session.ThoiGianKetThuc >= from && session.ThoiGianBatDau < to)
            .OrderBy(session => session.ThoiGianBatDau)
            .Select(session => new
            {
                session.CaThiId,
                session.KyThiId,
                MaKyThi = session.KyThi.MaKyThi,
                TenKyThi = session.KyThi.TenKyThi,
                MaPhong = session.PhongThi.MaPhong,
                session.ThoiGianBatDau,
                session.ThoiGianKetThuc,
                session.SucChua,
                session.TrangThai
            })
            .ToListAsync();

        return Ok(new
        {
            userCount,
            sessions = sessions.Select(session => new
            {
                session.CaThiId,
                session.KyThiId,
                session.MaKyThi,
                session.TenKyThi,
                session.MaPhong,
                session.ThoiGianBatDau,
                session.ThoiGianKetThuc,
                session.SucChua,
                TrangThai = session.TrangThai.ToString()
            })
        });
    }
}