using System.Security.Claims;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSchedule.Api.Controllers;

[ApiController]
[Route("api/proctors")]
[Authorize]
public class ProctorsController : ControllerBase
{
    private readonly ProctorService _service;

    public ProctorsController(ProctorService service) => _service = service;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    private bool CanManage => User.IsInRole("Admin") || User.IsInRole("CBKT");
    private bool CanViewAll => CanManage || User.IsInRole("QuanLy");

    [HttpGet]
    [Authorize(Policy = "CanViewExam")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        [FromQuery] int? caThiId = null)
        => Ok(await _service.GetProctorsAsync(keyword, status, caThiId));

    [HttpPost]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Create([FromBody] CreateProctorProfileRequest request)
        => Ok(await _service.CreateProfileAsync(request, CurrentUserId, ClientIp));

    [HttpGet("{proctorProfileId:int}/schedule")]
    public async Task<IActionResult> GetSchedule(int proctorProfileId)
    {
        var userId = CanViewAll ? (int?)null : CurrentUserId;
        return Ok(await _service.GetScheduleAsync(proctorProfileId, userId));
    }

    [HttpGet("me/schedule")]
    public async Task<IActionResult> GetMySchedule()
    {
        var profile = (await _service.GetProctorsAsync(null, null, null))
            .FirstOrDefault(p => p.UserId == CurrentUserId);
        if (profile == null)
            return Ok(Array.Empty<ProctorScheduleItemDto>());
        return Ok(await _service.GetScheduleAsync(profile.ProctorProfileId, CurrentUserId));
    }

    [HttpGet("assignments/{assignmentId:int}")]
    [Authorize(Policy = "CanViewExam")]
    public async Task<IActionResult> GetAssignment(int assignmentId)
    {
        var summary = await _service.GetAssignmentAsync(assignmentId);
        return Ok(summary);
    }

    [HttpPost("assign")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Assign([FromBody] AssignProctorRequest request)
        => Ok(await _service.AssignAsync(request, CurrentUserId, ClientIp));

    [HttpDelete("assignments/{assignmentId:int}")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Unassign(int assignmentId)
    {
        await _service.UnassignAsync(assignmentId, CurrentUserId, ClientIp);
        return NoContent();
    }

    [HttpPut("assignments/{assignmentId:int}")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Replace(
        int assignmentId, [FromBody] ReplaceProctorRequest request)
        => Ok(await _service.ReplaceAsync(assignmentId, request, CurrentUserId, ClientIp));

    [HttpPatch("assignments/{assignmentId:int}/role")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> UpdateRole(
        int assignmentId, [FromBody] UpdateProctorRoleRequest request)
        => Ok(await _service.UpdateRoleAsync(assignmentId, request, CurrentUserId, ClientIp));

    [HttpPost("sessions/{caThiId:int}/auto-assign")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> AutoAssign(int caThiId)
        => Ok(await _service.AutoAssignAsync(caThiId, CurrentUserId, ClientIp));
}

[ApiController]
[Route("api/examsessions/{caThiId:int}/proctors")]
[Authorize(Policy = "CanViewExam")]
public class ExamSessionProctorsController : ControllerBase
{
    private readonly ProctorService _service;

    public ExamSessionProctorsController(ProctorService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get(int caThiId)
        => Ok(await _service.GetSessionAssignmentsAsync(caThiId));
}