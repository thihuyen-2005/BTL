using System.Security.Claims;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSchedule.Api.Controllers;

[ApiController]
[Route("api/exams")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly ExamService _svc;
    public ExamsController(ExamService svc) { _svc = svc; }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

    [HttpGet]
    [Authorize(Policy = "CanViewExam")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int limit = 20,
        [FromQuery] string? status = null)
        => Ok(await _svc.GetPagedAsync(page, limit, status));

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewExam")]
    public async Task<IActionResult> GetById(int id)
        => Ok(await _svc.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Create([FromBody] KyThiCreateDto dto)
    {
        var r = await _svc.CreateAsync(dto, CurrentUserId, ClientIp);
        return CreatedAtAction(nameof(GetById), new { id = r.KyThiId }, r);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Update(int id, [FromBody] KyThiUpdateDto dto)
    {
        await _svc.UpdateAsync(id, dto, CurrentUserId, ClientIp);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ClientIp);
        return NoContent();
    }

    [HttpPut("{id}/approve")]
    [Authorize(Policy = "CanApprove")]      // ← QuanLy + Admin
    public async Task<IActionResult> Approve(int id)
    {
        return Ok(new { message = $"Đã phê duyệt kỳ thi {id}" });
    }
}