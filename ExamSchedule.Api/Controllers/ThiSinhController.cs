using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamSchedule.Api.Controllers;

[ApiController]
[Route("api/thisinh")]
[Authorize]
public class ThiSinhController : ControllerBase
{
    private readonly ThiSinhService _service;
    private readonly XepLichService _xepLichService;

    public ThiSinhController(ThiSinhService service, XepLichService xepLichService)
    {
        _service = service;
        _xepLichService = xepLichService;
    }

    [HttpGet]
    [Authorize(Policy = "CanViewExam")]
    public async Task<IActionResult> GetAll([FromQuery] string? tuKhoa = null, [FromQuery] string? lop = null,
        [FromQuery] string? nganhHoc = null, [FromQuery] string? khoa = null, [FromQuery] bool? daNop = null)
        => Ok(await _service.GetAllAsync(tuKhoa, lop, nganhHoc, khoa, daNop));

    [HttpPost]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Create([FromBody] ThiSinhCreateDto dto)
        => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Update(int id, [FromBody] ThiSinhUpdateDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Import([FromBody] List<ThiSinhImportDto> items)
        => Ok(new { soLuong = await _service.ImportAsync(items) });

    [HttpPost("import-excel")]
    [Authorize(Policy = "CanManageExam")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Vui lòng chọn file Excel." });
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Chỉ hỗ trợ file Excel .xlsx." });

        await using var stream = file.OpenReadStream();
        return Ok(await _service.ImportExcelAsync(stream));
    }

    [HttpPost("dang-ky")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Register([FromBody] DangKyThiCreateDto dto)
        => Ok(await _service.RegisterAsync(dto));

    [HttpGet("dang-ky")]
    [Authorize(Policy = "CanViewExam")]
    public async Task<IActionResult> GetRegistrations([FromQuery] int kyThiId, [FromQuery] string? trangThai = null)
        => Ok(await _service.GetRegistrationsAsync(kyThiId, trangThai));

    [HttpPost("ky-thi/{kyThiId}/xep-lich")]
    [Authorize(Policy = "CanManageExam")]
    public async Task<IActionResult> Schedule(int kyThiId)
        => Ok(await _xepLichService.XepLichAsync(kyThiId));
}