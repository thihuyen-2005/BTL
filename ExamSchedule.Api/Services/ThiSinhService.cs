using ExamSchedule.Api.Data;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Entities;
using ExamSchedule.Api.Middlewares;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace ExamSchedule.Api.Services;

public class ThiSinhService
{
    private readonly AppDbContext _db;

    public ThiSinhService(AppDbContext db) => _db = db;

    public async Task<List<ThiSinhResponseDto>> GetAllAsync(string? tuKhoa, string? lop, string? nganhHoc, string? khoa)
    {
        var query = _db.ThiSinhs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
            query = query.Where(x => x.MaThiSinh.Contains(tuKhoa) || x.HoTen.Contains(tuKhoa));
        if (!string.IsNullOrWhiteSpace(lop)) query = query.Where(x => x.Lop == lop);
        if (!string.IsNullOrWhiteSpace(nganhHoc)) query = query.Where(x => x.NganhHoc == nganhHoc);
        if (!string.IsNullOrWhiteSpace(khoa)) query = query.Where(x => x.Khoa == khoa);

        return await query.OrderBy(x => x.HoTen)
            .Select(x => new ThiSinhResponseDto(
                x.ThiSinhId, x.MaThiSinh, x.HoTen, x.NgaySinh, x.GioiTinh,
                x.DanToc, x.NoiSinh, x.QuocTich, x.SoCccdHoChieu,
                x.SoDienThoai, x.Lop, x.NganhHoc, x.Khoa, x.SoTien, x.EmailCaNhan))
            .ToListAsync();
    }

    public async Task<int> ImportAsync(IEnumerable<ThiSinhImportDto> items)
    {
        var imported = 0;
        var candidates = await _db.ThiSinhs
            .ToDictionaryAsync(x => x.MaThiSinh, StringComparer.OrdinalIgnoreCase);
        foreach (var dto in items.Where(x => !string.IsNullOrWhiteSpace(x.MaThiSinh) && !string.IsNullOrWhiteSpace(x.HoTen)))
        {
            var code = NormalizeCode(dto.MaThiSinh);
            if (!candidates.TryGetValue(code, out var candidate))
            {
                candidate = new ThiSinh { MaThiSinh = code };
                _db.ThiSinhs.Add(candidate);
                candidates[code] = candidate;
            }
            Apply(candidate, dto with { MaThiSinh = code });
            imported++;
        }

        await _db.SaveChangesAsync();
        return imported;
    }

    public async Task<ThiSinhResponseDto> CreateAsync(ThiSinhCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MaThiSinh) || string.IsNullOrWhiteSpace(dto.HoTen))
            throw new BusinessException("Mã thí sinh và họ tên là bắt buộc.");

        await ImportAsync(new[] { (ThiSinhImportDto)dto });
        var candidate = await _db.ThiSinhs.AsNoTracking()
            .FirstAsync(x => x.MaThiSinh == dto.MaThiSinh.Trim());
        return ToDto(candidate);
    }

    public async Task<ThiSinhResponseDto> UpdateAsync(int id, ThiSinhUpdateDto dto)
    {
        var candidate = await _db.ThiSinhs.FindAsync(id)
            ?? throw new NotFoundException("Không tìm thấy thí sinh.");
        if (await _db.ThiSinhs.AnyAsync(x => x.ThiSinhId != id && x.MaThiSinh == dto.MaThiSinh.Trim()))
            throw new BusinessException("Mã thí sinh đã tồn tại.");
        Apply(candidate, dto);
        await _db.SaveChangesAsync();
        return ToDto(candidate);
    }

    public async Task DeleteAsync(int id)
    {
        var candidate = await _db.ThiSinhs.FindAsync(id)
            ?? throw new NotFoundException("Không tìm thấy thí sinh.");
        if (await _db.DangKyThis.AnyAsync(x => x.ThiSinhId == id))
            throw new BusinessException("Không thể xóa thí sinh đã có đăng ký thi.");
        _db.ThiSinhs.Remove(candidate);
        await _db.SaveChangesAsync();
    }

    public async Task<ThiSinhImportResultDto> ImportExcelAsync(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new BusinessException("File Excel không có sheet dữ liệu.");
        var used = sheet.RangeUsed()
            ?? throw new BusinessException("File Excel không có dữ liệu.");
        var headerRow = used.FirstRowUsed();
        var headers = headerRow.Cells().ToDictionary(
            cell => NormalizeHeader(cell.GetString()), cell => cell.Address.ColumnNumber);
        var errors = new List<string>();
        var items = new List<ThiSinhImportDto>();
        var rowNumber = headerRow.RowNumber() + 1;

        foreach (var row in used.RowsUsed().Skip(1))
        {
            if (row.Cells().All(cell => string.IsNullOrWhiteSpace(cell.GetFormattedString())))
                continue;
            try
            {
                var name = Read(row, headers, "ho va ten", "ho ten", "hoten");
                if (string.IsNullOrWhiteSpace(name))
                    throw new BusinessException("Thiếu họ tên.");
                var identity = Read(row, headers, "so cccd ho chieu", "cccd", "so cccd");
                var explicitCode = Read(row, headers, "ma thi sinh", "ma so");
                var ordinal = Read(row, headers, "tt");
                var code = !string.IsNullOrWhiteSpace(explicitCode)
                    ? explicitCode
                    : !string.IsNullOrWhiteSpace(identity)
                        ? $"TS-{identity}"
                        : !string.IsNullOrWhiteSpace(ordinal)
                            ? $"TS-{ordinal.PadLeft(4, '0')}"
                            : $"TS-{rowNumber:0000}";

                items.Add(new ThiSinhImportDto(
                    code, name, ReadDate(row, headers, "ngay sinh"),
                    Read(row, headers, "gioi tinh"), Read(row, headers, "dan toc"),
                    Read(row, headers, "noi sinh tinh tp", "noi sinh"),
                    Read(row, headers, "quoc tich"), identity,
                    Read(row, headers, "sdt", "so dien thoai", "dien thoai"),
                    Read(row, headers, "lop"), Read(row, headers, "nganh hoc"),
                    Read(row, headers, "khoa"), ReadMoney(row, headers, "so tien", "so tien2", "so tien 2", "hoc phi", "tien"),
                    Read(row, headers, "email ca nhan", "email")));
            }
            catch (Exception ex)
            {
                errors.Add($"Dòng {rowNumber}: {ex.Message}");
            }
            rowNumber++;
        }

        var imported = await ImportAsync(items);
        return new ThiSinhImportResultDto(imported, errors);
    }

    public async Task<DangKyThiResponseDto> RegisterAsync(DangKyThiCreateDto dto)
    {
        var candidate = await _db.ThiSinhs.FirstOrDefaultAsync(x => x.MaThiSinh == dto.MaThiSinh)
            ?? throw new NotFoundException("Không tìm thấy thí sinh.");
        var exam = await _db.KyThis.FindAsync(dto.KyThiId)
            ?? throw new NotFoundException("Không tìm thấy kỳ thi.");
        if (exam.TrangThai is TrangThaiKyThi.KetThuc or TrangThaiKyThi.Huy)
            throw new BusinessException("Kỳ thi đã kết thúc hoặc bị hủy.");
        if (await _db.DangKyThis.AnyAsync(x => x.ThiSinhId == candidate.ThiSinhId && x.KyThiId == dto.KyThiId))
            throw new BusinessException("Thí sinh đã đăng ký kỳ thi này.");

        var hasPaid = candidate.SoTien == 800000m;
        var registration = new DangKyThi
        {
            ThiSinhId = candidate.ThiSinhId,
            KyThiId = dto.KyThiId,
            TrangThai = hasPaid ? TrangThaiDangKyThi.ChoXep : TrangThaiDangKyThi.ChuaXep,
            LyDoChuaXep = hasPaid ? null : "Chưa nộp đủ lệ phí 800.000 đồng."
        };
        _db.DangKyThis.Add(registration);
        await _db.SaveChangesAsync();
        await LoadRegistrationAsync(registration);
        return ToRegistrationDto(registration);
    }

    public async Task<List<DangKyThiResponseDto>> GetRegistrationsAsync(int kyThiId, string? trangThai)
    {
        var query = _db.DangKyThis.Include(x => x.ThiSinh).Include(x => x.CaThi).AsNoTracking()
            .Where(x => x.KyThiId == kyThiId);
        if (!string.IsNullOrWhiteSpace(trangThai) && Enum.TryParse<TrangThaiDangKyThi>(trangThai, out var status))
            query = query.Where(x => x.TrangThai == status);
        return (await query.OrderBy(x => x.ThiSinh.HoTen).ToListAsync()).Select(ToRegistrationDto).ToList();
    }

    private async Task LoadRegistrationAsync(DangKyThi registration)
    {
        await _db.Entry(registration).Reference(x => x.ThiSinh).LoadAsync();
        if (registration.CaThiId.HasValue)
            await _db.Entry(registration).Reference(x => x.CaThi).LoadAsync();
    }

    private static ThiSinhResponseDto ToDto(ThiSinh x) => new(
        x.ThiSinhId, x.MaThiSinh, x.HoTen, x.NgaySinh, x.GioiTinh, x.DanToc,
        x.NoiSinh, x.QuocTich, x.SoCccdHoChieu, x.SoDienThoai, x.Lop,
        x.NganhHoc, x.Khoa, x.SoTien, x.EmailCaNhan);

    private static string NormalizeCode(string code)
    {
        var value = code.Trim();
        return int.TryParse(value, out var ordinal) ? $"TS-{ordinal:0000}" : value;
    }

    private static void Apply(ThiSinh candidate, ThiSinhImportDto dto)
    {
        candidate.MaThiSinh = dto.MaThiSinh.Trim();
        candidate.HoTen = dto.HoTen.Trim();
        candidate.NgaySinh = dto.NgaySinh;
        candidate.GioiTinh = dto.GioiTinh;
        candidate.DanToc = dto.DanToc;
        candidate.NoiSinh = dto.NoiSinh;
        candidate.QuocTich = dto.QuocTich;
        candidate.SoCccdHoChieu = dto.SoCccdHoChieu;
        candidate.SoDienThoai = dto.SoDienThoai;
        candidate.Lop = dto.Lop;
        candidate.NganhHoc = dto.NganhHoc;
        candidate.Khoa = dto.Khoa;
        candidate.SoTien = dto.SoTien;
        candidate.EmailCaNhan = dto.EmailCaNhan;
    }

    private static string? Read(IXLRangeRow row, IReadOnlyDictionary<string, int> headers, params string[] names)
    {
        foreach (var name in names)
            if (headers.TryGetValue(NormalizeHeader(name), out var column))
            {
                var value = row.Cell(column).GetFormattedString().Trim();
                return value.Length == 0 ? null : value;
            }
        return null;
    }

    private static DateTime? ReadDate(IXLRangeRow row, IReadOnlyDictionary<string, int> headers, params string[] names)
    {
        var value = Read(row, headers, names);
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, new CultureInfo("vi-VN"), DateTimeStyles.None, out var date)) return date;
        if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return date;
        throw new BusinessException($"Ngày sinh không hợp lệ: {value}");
    }

    private static decimal? ReadMoney(IXLRangeRow row, IReadOnlyDictionary<string, int> headers, params string[] names)
    {
        var value = Read(row, headers, names);
        if (string.IsNullOrWhiteSpace(value) || value.Contains("chưa nộp", StringComparison.OrdinalIgnoreCase)) return null;
        var normalized = value.Replace("đ", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".", "").Replace(",", ".").Trim();
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var money)) return money;
        throw new BusinessException($"Số tiền không hợp lệ: {value}");
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant()
            .Replace("/", " ").Replace("(", "").Replace(")", "").Replace("-", " ")
            .Replace("_", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries) is var parts
            ? string.Join(' ', parts)
            : value.ToLowerInvariant();
    }

    public static DangKyThiResponseDto ToRegistrationDto(DangKyThi x) => new(
        x.DangKyThiId, x.ThiSinhId, x.ThiSinh?.MaThiSinh ?? "", x.ThiSinh?.HoTen ?? "",
        x.KyThiId, x.CaThiId, x.TrangThai.ToString(), x.LyDoChuaXep,
        x.CaThi?.ThoiGianBatDau, x.CaThi?.ThoiGianKetThuc, x.CaThi?.PhongThi?.MaPhong);
}