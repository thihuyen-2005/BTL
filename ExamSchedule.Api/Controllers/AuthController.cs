using System.Security.Claims;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSchedule.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) { _auth = auth; }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var res = await _auth.LoginAsync(req);
            return Ok(res);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        try { return Ok(await _auth.RefreshAsync(req.RefreshToken)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _auth.GetCurrentUserAsync(id));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _auth.LogoutAsync(id);
        return NoContent();
    }
    
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _auth.ChangePasswordAsync(id, req);
        return Ok(new { message = "Đổi mật khẩu thành công!" });
    }

    [HttpGet("test-hash")]
[AllowAnonymous]
public IActionResult TestHash()
{
    var hash = BCrypt.Net.BCrypt.HashPassword("Admin@2026");
    var verify = BCrypt.Net.BCrypt.Verify("Admin@2026", hash);
    var verifyWrong = BCrypt.Net.BCrypt.Verify("admin@2026", hash);
    return Ok(new { 
        hash, 
        verifyWithCorrectPw = verify,     // Phải = true
        verifyWithWrongPw = verifyWrong   // Phải = false
    });
}
}