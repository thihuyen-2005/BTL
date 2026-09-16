using System.Security.Claims;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSchedule.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "IsAdmin")]     
public class UsersController : ControllerBase
{
    private readonly UserService _svc;
    public UsersController(UserService svc) { _svc = svc; }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _svc.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        => Ok(await _svc.CreateAsync(req, CurrentUserId, ClientIp));

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest req)
    {
        await _svc.ResetPasswordAsync(id, req.NewPassword, CurrentUserId, ClientIp);
        return Ok(new { message = "Đã reset password." });
    }

    [HttpPut("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        await _svc.ToggleActiveAsync(id, CurrentUserId, ClientIp);
        return Ok(new { message = "Đã đổi trạng thái." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ClientIp);
        return NoContent();
    }
}