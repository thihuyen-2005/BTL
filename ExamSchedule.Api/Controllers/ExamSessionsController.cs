using System.Security.Claims;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSchedule.Api.Controllers;

[ApiController]
[Route("api/exam-sessions")]
[Authorize]
public class ExamSessionsController : ControllerBase
{
    private readonly ExamSessionService _svc;
    public ExamSessionsController(ExamSessionService svc) { _svc = svc; }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

    [HttpGet]
    [Authorize(Policy = "CanViewExam")]
    public async Task<IActionResult> GetByKyThi(
        [FromQuery] int kyThiId, [FromQuery] string? trangThai = null,
        [FromQuery] string? keyword = null)
        => Ok(await _svc.GetByKyThiAsync(kyThiId, trangThai, keyword));

    [HttpPost]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Create([FromBody] CaThiCreateDto dto)
        => Ok(await _svc.CreateAsync(dto, CurrentUserId, ClientIp));

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Update(int id, [FromBody] CaThiUpdateDto dto)
    {
        await _svc.UpdateAsync(id, dto, CurrentUserId, ClientIp);
        return NoContent();
    }

    [HttpPut("{id}/close")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Close(int id)
    {
        await _svc.CloseAsync(id, CurrentUserId, ClientIp);
        return Ok(new { message = "Đã đóng ca thi." });
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _svc.CancelAsync(id, CurrentUserId, ClientIp);
        return Ok(new { message = "Đã hủy ca thi." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ClientIp);
        return NoContent();
    }
}