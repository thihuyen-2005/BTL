using ExamSchedule.Api.Data;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Entities;
using ExamSchedule.Api.Middlewares;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ExamSchedule.Api.Services;

public class ThiSinhService
{
    // Dữ liệu đang lưu trong DB là 800 cho trường hợp đã nộp, không phải 800000.
    // Do đó rule đúng với database hiện tại phải là >= 800.
    private const decimal MucNopToiThieu = 800m;
    private static readonly string[] DefaultPhonePool =
    {
        "0399480863", "0817707295", "0969868573", "0374151236", "0978228605",
        "0855934189", "0325673402", "0707246208", "0326893801", "0397262094",
        "0376605012", "0395328102", "0328729735", "0344470600", "0369372956",
        "0332010640", "0357650284", "0389723143", "0362565854", "0367694963",
        "0869010933", "0989229926"
    };
    private readonly AppDbContext _db;

    public ThiSinhService(AppDbContext db) => _db = db;

    public static bool DaNop(decimal? soTien) => soTien.HasValue && soTien.Value >= MucNopToiThieu;
    public static bool ChuaNop(decimal? soTien) => !soTien.HasValue || soTien.Value < MucNopToiThieu;

    public async Task<List<ThiSinhResponseDto>> GetAllAsync(string? tuKhoa, string? lop, string? nganhHoc, string? khoa, bool? daNop = null)
    {
        var query = _db.ThiSinhs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
            query = query.Where(x =>
                (x.MaThiSinh != null && x.MaThiSinh.Contains(tuKhoa)) ||
                (x.HoTen != null && x.HoTen.Contains(tuKhoa)) ||
                (x.NganhHoc != null && x.NganhHoc.Contains(tuKhoa)));
        if (!string.IsNullOrWhiteSpace(lop)) query = query.Where(x => x.Lop == lop);

        var normalizedKhoa = AcademicCatalog.NormalizeKhoa(khoa);
        var normalizedNganh = AcademicCatalog.NormalizeNganhHoc(normalizedKhoa, nganhHoc);

        if (daNop.HasValue)
        {
            if (daNop.Value)
                query = query.Where(x => x.SoTien.HasValue && x.SoTien.Value >= MucNopToiThieu);
            else
                query = query.Where(x => !x.SoTien.HasValue || x.SoTien.Value < MucNopToiThieu);
        }

        var items = await query.ToListAsync();
        if (!string.IsNullOrWhiteSpace(normalizedKhoa))
            items = items
                .Where(x => x.Khoa == normalizedKhoa || AcademicCatalog.NormalizeKhoa(x.Khoa) == normalizedKhoa)
                .ToList();

        if (!string.IsNullOrWhiteSpace(normalizedNganh))
            items = items
                .Where(x => x.NganhHoc == normalizedNganh || AcademicCatalog.NormalizeNganhHoc(x.Khoa, x.NganhHoc) == normalizedNganh)
                .ToList();

        return items
            .OrderBy(x => RemoveDiacritics(x.HoTen ?? string.Empty))
            .ThenBy(x => x.HoTen ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(x => new ThiSinhResponseDto(
                x.ThiSinhId, x.MaThiSinh, x.HoTen, x.NgaySinh, x.GioiTinh,
                x.DanToc, x.NoiSinh, x.QuocTich, x.SoCccdHoChieu,
                x.SoDienThoai, x.Lop, x.NganhHoc, x.Khoa, x.SoTien, x.EmailCaNhan))
            .ToList();
    }

    public async Task<int> ImportAsync(IEnumerable<ThiSinhImportDto> items)
    {
        var imported = 0;
        var candidates = await _db.ThiSinhs
            .ToDictionaryAsync(x => x.MaThiSinh, StringComparer.OrdinalIgnoreCase);
        foreach (var dto in items.Where(x => !string.IsNullOrWhiteSpace(x.MaThiSinh) && !string.IsNullOrWhiteSpace(x.HoTen)))
        {
            var code = NormalizeCode(dto.MaThiSinh!);
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

    public async Task NormalizeDataIntegrityAsync()
    {
        var candidates = await _db.ThiSinhs.OrderBy(x => x.ThiSinhId).ToListAsync();

        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var normalized = NormalizeDisplayCode(candidate.MaThiSinh);
            if (!string.IsNullOrWhiteSpace(normalized))
                usedCodes.Add(normalized);
        }

        var nextCodeNumber = 1;
        foreach (var candidate in candidates)
        {
            var existing = NormalizeDisplayCode(candidate.MaThiSinh);
            if (!string.IsNullOrWhiteSpace(existing) && IsCanonicalShortCode(existing) && !usedCodes.Where(x => x.Equals(existing, StringComparison.OrdinalIgnoreCase)).Skip(1).Any())
            {
                candidate.MaThiSinh = existing;
                continue;
            }

            while (usedCodes.Contains($"TS{nextCodeNumber:D4}", StringComparer.OrdinalIgnoreCase))
                nextCodeNumber++;

            var newCode = $"TS{nextCodeNumber:D4}";
            Console.WriteLine($"[ThiSinhIntegrity] Đổi mã thí sinh {candidate.MaThiSinh} -> {newCode}; ThiSinhId={candidate.ThiSinhId}");
            candidate.MaThiSinh = newCode;
            usedCodes.Add(newCode);
            nextCodeNumber++;
        }

        var usedPhones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var phone = NormalizePhoneNumber(candidate.SoDienThoai);
            if (!string.IsNullOrWhiteSpace(phone) && IsValidPhoneNumber(phone))
                usedPhones.Add(phone);
        }

        foreach (var candidate in candidates)
        {
            var phone = NormalizePhoneNumber(candidate.SoDienThoai);
            if (string.IsNullOrWhiteSpace(phone) || !IsValidPhoneNumber(phone))
            {
                var fallback = GetNextAvailablePhoneNumber(usedPhones, DefaultPhonePool);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    candidate.SoDienThoai = fallback;
                    usedPhones.Add(fallback);
                    Console.WriteLine($"[ThiSinhIntegrity] Gán số điện thoại {fallback} cho ThiSinhId={candidate.ThiSinhId}");
                }
            }
            else
            {
                candidate.SoDienThoai = phone;
                usedPhones.Add(phone);
            }

            if (string.IsNullOrWhiteSpace(candidate.HoTen))
            {
                candidate.HoTen = $"Thí sinh {candidate.ThiSinhId}";
                Console.WriteLine($"[ThiSinhIntegrity] Bổ sung họ tên cho ThiSinhId={candidate.ThiSinhId}");
            }
            if (!candidate.NgaySinh.HasValue)
            {
                candidate.NgaySinh = new DateTime(1998, 1, 1);
                Console.WriteLine($"[ThiSinhIntegrity] Bổ sung ngày sinh cho ThiSinhId={candidate.ThiSinhId}");
            }
            if (string.IsNullOrWhiteSpace(candidate.GioiTinh))
            {
                candidate.GioiTinh = "Khác";
                Console.WriteLine($"[ThiSinhIntegrity] Bổ sung giới tính cho ThiSinhId={candidate.ThiSinhId}");
            }
            if (string.IsNullOrWhiteSpace(candidate.Lop))
            {
                candidate.Lop = "Chưa cập nhật";
                Console.WriteLine($"[ThiSinhIntegrity] Bổ sung lớp cho ThiSinhId={candidate.ThiSinhId}");
            }
            if (string.IsNullOrWhiteSpace(candidate.Khoa))
            {
                candidate.Khoa = "Chưa cập nhật";
                Console.WriteLine($"[ThiSinhIntegrity] Bổ sung khoa cho ThiSinhId={candidate.ThiSinhId}");
            }
            if (string.IsNullOrWhiteSpace(candidate.NganhHoc))
            {
                candidate.NganhHoc = "Chưa cập nhật";
                Console.WriteLine($"[ThiSinhIntegrity] Bổ sung ngành học cho ThiSinhId={candidate.ThiSinhId}");
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<ThiSinhResponseDto> CreateAsync(ThiSinhCreateDto dto)
    {
        dto = dto with
        {
            MaThiSinh = string.IsNullOrWhiteSpace(dto.MaThiSinh) ? null : NormalizeCode(dto.MaThiSinh.Trim()),
            SoDienThoai = NormalizePhoneNumber(dto.SoDienThoai)
        };

        ValidateRequiredFields(dto);

        await EnsureUniquePhoneNumberAsync(dto.SoDienThoai, null);

        var maThiSinh = string.IsNullOrWhiteSpace(dto.MaThiSinh)
            ? await GenerateNewMaThiSinhAsync()
            : NormalizeDisplayCode(dto.MaThiSinh);

        if (await _db.ThiSinhs.AnyAsync(x => x.MaThiSinh == maThiSinh))
            throw new BusinessException("Mã thí sinh đã tồn tại.");

        var candidate = new ThiSinh { MaThiSinh = maThiSinh };
        Apply(candidate, dto with { MaThiSinh = maThiSinh });
        _db.ThiSinhs.Add(candidate);
        await _db.SaveChangesAsync();
        return ToDto(candidate);
    }

    public async Task<ThiSinhResponseDto> UpdateAsync(int id, ThiSinhUpdateDto dto)
    {
        dto = dto with
        {
            MaThiSinh = string.IsNullOrWhiteSpace(dto.MaThiSinh) ? null : NormalizeCode(dto.MaThiSinh.Trim()),
            SoDienThoai = NormalizePhoneNumber(dto.SoDienThoai)
        };

        ValidateRequiredFields(dto);

        var candidate = await _db.ThiSinhs.FindAsync(id)
            ?? throw new NotFoundException("Không tìm thấy thí sinh.");

        var normalizedMaThiSinh = string.IsNullOrWhiteSpace(dto.MaThiSinh)
            ? candidate.MaThiSinh
            : NormalizeDisplayCode(dto.MaThiSinh.Trim());

        if (await _db.ThiSinhs.AnyAsync(x => x.ThiSinhId != id && x.MaThiSinh == normalizedMaThiSinh))
            throw new BusinessException("Mã thí sinh đã tồn tại.");

        await EnsureUniquePhoneNumberAsync(dto.SoDienThoai, id);

        dto = dto with { MaThiSinh = normalizedMaThiSinh };
        Apply(candidate, dto);
        await _db.SaveChangesAsync();
        return ToDto(candidate);
    }

    public async Task DeleteAsync(int id)
    {
        var candidate = await _db.ThiSinhs
            .Include(x => x.DangKyThis)
            .FirstOrDefaultAsync(x => x.ThiSinhId == id)
            ?? throw new NotFoundException("Không tìm thấy thí sinh.");

        if (candidate.DangKyThis.Count > 0)
        {
            var regIds = candidate.DangKyThis.Select(x => x.DangKyThiId).ToList();
            var registrations = await _db.DangKyThis
                .Where(x => regIds.Contains(x.DangKyThiId))
                .ToListAsync();
            _db.DangKyThis.RemoveRange(registrations);
        }

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

        var hasPaid = DaNop(candidate.SoTien);
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

    public async Task<List<ThiSinhScheduleCandidateDto>> GetManualScheduleCandidatesAsync(int kyThiId, List<int>? selectedThiSinhIds = null)
    {
        var query = _db.DangKyThis
            .Include(x => x.ThiSinh)
            .Include(x => x.CaThi).ThenInclude(x => x!.PhongThi)
            .AsNoTracking()
            .Where(x => x.KyThiId == kyThiId);

        if (selectedThiSinhIds != null && selectedThiSinhIds.Count > 0)
        {
            query = query.Where(x => selectedThiSinhIds.Contains(x.ThiSinhId));
        }

        var registrations = await query
            .OrderBy(x => x.ThiSinh.HoTen)
            .ThenBy(x => x.ThiSinhId)
            .ToListAsync();

        return registrations.Select(x =>
        {
            var paid = x.ThiSinh.SoTien.HasValue && x.ThiSinh.SoTien.Value >= MucNopToiThieu;
            var alreadyScheduled = x.CaThiId.HasValue && x.TrangThai == TrangThaiDangKyThi.DaXep;
            var canSchedule = paid && !alreadyScheduled;
            return new ThiSinhScheduleCandidateDto(
                x.DangKyThiId,
                x.ThiSinhId,
                x.ThiSinh.MaThiSinh ?? "",
                x.ThiSinh.HoTen ?? "",
                x.ThiSinh.Lop,
                x.ThiSinh.Khoa,
                x.ThiSinh.NganhHoc,
                x.ThiSinh.SoTien,
                x.TrangThai.ToString(),
                x.LyDoChuaXep,
                x.CaThiId,
                x.CaThi?.ThoiGianBatDau,
                x.CaThi?.ThoiGianKetThuc,
                x.CaThi?.PhongThi?.MaPhong,
                canSchedule,
                !paid ? "Chưa nộp đủ lệ phí 800.000 đồng" : alreadyScheduled ? "Đã có ca thi" : "Sẵn sàng xếp lịch");
        }).ToList();
    }

    public async Task<List<ThiSinhScheduleCandidateDto>> AssignManualScheduleAsync(int kyThiId, int caThiId, List<int> thiSinhIds)
    {
        if (thiSinhIds == null || thiSinhIds.Count == 0)
            return await GetManualScheduleCandidatesAsync(kyThiId);

        var exam = await _db.KyThis.FindAsync(kyThiId)
            ?? throw new NotFoundException("Không tìm thấy kỳ thi.");
        if (exam.TrangThai is TrangThaiKyThi.KetThuc or TrangThaiKyThi.Huy)
            throw new BusinessException("Kỳ thi đã kết thúc hoặc bị hủy.");

        var session = await _db.CaThis.FirstOrDefaultAsync(x => x.CaThiId == caThiId && x.KyThiId == kyThiId)
            ?? throw new NotFoundException("Không tìm thấy ca thi trong kỳ thi được chọn.");

        var registrations = await _db.DangKyThis
            .Include(x => x.ThiSinh)
            .Where(x => x.KyThiId == kyThiId && thiSinhIds.Contains(x.ThiSinhId))
            .ToListAsync();

        if (registrations.Count != thiSinhIds.Distinct().Count())
            throw new BusinessException("Một số thí sinh không thuộc kỳ thi được chọn.");

        var currentOccupied = await _db.DangKyThis.CountAsync(x => x.CaThiId == caThiId && x.TrangThai == TrangThaiDangKyThi.DaXep && x.KyThiId == kyThiId);
        var incomingCount = thiSinhIds.Distinct().Count();
        if (currentOccupied + incomingCount > session.SucChua)
            throw new BusinessException($"Ca thi đã đầy. Sức chứa hiện tại: {session.SucChua}; còn {session.SucChua - currentOccupied} chỗ.");

        foreach (var registration in registrations)
        {
            if (!registration.ThiSinh.SoTien.HasValue || registration.ThiSinh.SoTien.Value < MucNopToiThieu)
                throw new BusinessException($"Thí sinh {registration.ThiSinh.HoTen} chưa nộp đủ lệ phí 800.000 đồng.");

            registration.CaThiId = caThiId;
            registration.TrangThai = TrangThaiDangKyThi.DaXep;
            registration.LyDoChuaXep = null;
            registration.NgayCapNhat = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return await GetManualScheduleCandidatesAsync(kyThiId);
    }

    public async Task<List<ThiSinhScheduleCandidateDto>> RemoveManualScheduleAsync(int kyThiId, int thiSinhId)
    {
        var registration = await _db.DangKyThis
            .Include(x => x.ThiSinh)
            .FirstOrDefaultAsync(x => x.KyThiId == kyThiId && x.ThiSinhId == thiSinhId)
            ?? throw new NotFoundException("Không tìm thấy đăng ký thí sinh trong kỳ thi này.");

        registration.CaThiId = null;
        registration.TrangThai = registration.ThiSinh.SoTien.HasValue && registration.ThiSinh.SoTien.Value >= MucNopToiThieu
            ? TrangThaiDangKyThi.ChoXep
            : TrangThaiDangKyThi.ChuaXep;
        registration.LyDoChuaXep = (registration.ThiSinh.SoTien.HasValue && registration.ThiSinh.SoTien.Value >= MucNopToiThieu)
            ? null
            : "Chưa nộp đủ lệ phí 800.000 đồng.";
        registration.NgayCapNhat = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetManualScheduleCandidatesAsync(kyThiId);
    }

    private async Task LoadRegistrationAsync(DangKyThi registration)
    {
        await _db.Entry(registration).Reference(x => x.ThiSinh).LoadAsync();
        if (registration.CaThiId.HasValue)
            await _db.Entry(registration).Reference(x => x.CaThi).LoadAsync();
    }

    private async Task<string> GenerateNewMaThiSinhAsync()
    {
        var codes = await _db.ThiSinhs.AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.MaThiSinh) && x.MaThiSinh.StartsWith("TS", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.MaThiSinh)
            .ToListAsync();

        var maxNumber = 0;
        foreach (var code in codes)
        {
            var digits = Regex.Replace(code, "\\D", "");
            if (digits.Length == 4 && int.TryParse(digits, out var n))
                maxNumber = Math.Max(maxNumber, n);
        }

        if (maxNumber >= 9999)
            throw new BusinessException("Đã đạt giới hạn mã thí sinh TS9999. Không thể tạo thêm mã mới.");

        return $"TS{maxNumber + 1:D4}";
    }

    private async Task EnsureUniquePhoneNumberAsync(string? phone, int? ignoreId)
    {
        var normalizedPhone = NormalizePhoneNumber(phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
            throw new BusinessException("Số điện thoại không được để trống.");
        if (!IsValidPhoneNumber(normalizedPhone))
            throw new BusinessException("Số điện thoại không hợp lệ. Phải đúng 10 số và bắt đầu bằng 03, 05, 07, 08 hoặc 09.");

        var hasDuplicate = await _db.ThiSinhs.AnyAsync(x =>
            x.ThiSinhId != (ignoreId ?? -1) &&
            !string.IsNullOrWhiteSpace(x.SoDienThoai) &&
            x.SoDienThoai == normalizedPhone);

        if (hasDuplicate)
            throw new BusinessException("Số điện thoại đã tồn tại trong hệ thống.");
    }

    private static void ValidateRequiredFields(ThiSinhImportDto dto)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.HoTen)) missing.Add("Họ và tên");
        if (!dto.NgaySinh.HasValue) missing.Add("Ngày sinh");
        if (string.IsNullOrWhiteSpace(dto.GioiTinh)) missing.Add("Giới tính");
        if (string.IsNullOrWhiteSpace(dto.SoDienThoai)) missing.Add("Số điện thoại");
        if (string.IsNullOrWhiteSpace(dto.Lop)) missing.Add("Lớp");
        if (string.IsNullOrWhiteSpace(dto.NganhHoc)) missing.Add("Ngành học");
        if (string.IsNullOrWhiteSpace(dto.Khoa)) missing.Add("Khoa");

        if (missing.Count > 0)
            throw new BusinessException($"Thiếu thông tin bắt buộc: {string.Join(", ", missing)}.");
    }

    private static ThiSinhResponseDto ToDto(ThiSinh x) => new(
        x.ThiSinhId, x.MaThiSinh, x.HoTen, x.NgaySinh, x.GioiTinh, x.DanToc,
        x.NoiSinh, x.QuocTich, x.SoCccdHoChieu, x.SoDienThoai, x.Lop,
        x.NganhHoc, x.Khoa, x.SoTien, x.EmailCaNhan);

    private static string NormalizeCode(string? code)
    {
        var value = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var match = Regex.Match(value, "(?i)^ts\\s*[- ]?\\s*(\\d{1,4})$");
        if (match.Success)
        {
            var number = int.Parse(match.Groups[1].Value);
            return $"TS{number:D4}";
        }

        var digitsOnly = Regex.Replace(value, "\\D", "");
        if (string.IsNullOrWhiteSpace(digitsOnly)) return value;
        if (digitsOnly.Length <= 4 && int.TryParse(digitsOnly, out var shortNumber))
            return $"TS{shortNumber:D4}";

        return string.Empty;
    }

    private static string NormalizeDisplayCode(string? code)
    {
        var value = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var match = Regex.Match(value, "(?i)^ts\\s*[- ]?\\s*(\\d{1,4})$");
        if (match.Success)
        {
            var number = int.Parse(match.Groups[1].Value);
            return $"TS{number:D4}";
        }

        var digitsOnly = Regex.Replace(value, "\\D", "");
        if (digitsOnly.Length <= 4 && int.TryParse(digitsOnly, out var shortNumber))
            return $"TS{shortNumber:D4}";

        return string.Empty;
    }

    private static bool IsCanonicalShortCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return Regex.IsMatch(code, "(?i)^TS\\d{4}$") && int.TryParse(code[2..], out var value) && value >= 1 && value <= 9999;
    }

    private static string GenerateUniqueCode(string? rawCode, ISet<string> usedCodes)
    {
        var value = (rawCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
            return GetNextAvailableCode(usedCodes);

        var legacyMatch = Regex.Match(value, "(?i)^ts[-\\s]*?\\d{7,}$");
        if (legacyMatch.Success)
            return GetNextAvailableCode(usedCodes);

        var normalized = NormalizeDisplayCode(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return GetNextAvailableCode(usedCodes);

        if (!usedCodes.Contains(normalized) || string.Equals(normalized, value, StringComparison.OrdinalIgnoreCase))
            return normalized;

        return GetNextAvailableCode(usedCodes);
    }

    private static string GetNextAvailableCode(ISet<string> usedCodes)
    {
        var next = 1;
        while (usedCodes.Contains($"TS{next:D4}"))
            next++;
        return $"TS{next:D4}";
    }

    private static string NormalizePhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        var normalized = Regex.Replace(phone.Trim(), "\\D", "");
        if (normalized.Length == 11 && normalized.StartsWith("84"))
            normalized = "0" + normalized[2..];
        return normalized;
    }

    private static bool IsValidPhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var normalized = NormalizePhoneNumber(phone);
        return normalized.Length == 10 && Regex.IsMatch(normalized, "^0(3|5|7|8|9)\\d{8}$");
    }

    private static string GetNextAvailablePhoneNumber(ISet<string> usedPhones, IEnumerable<string>? preferredPool = null)
    {
        var pool = preferredPool ?? DefaultPhonePool;
        foreach (var candidate in pool)
        {
            var normalized = NormalizePhoneNumber(candidate);
            if (!string.IsNullOrWhiteSpace(normalized) && !usedPhones.Contains(normalized))
                return normalized;
        }

        var prefixes = new[] { "03", "05", "07", "08", "09" };
        for (var i = 0; i < 1000000; i++)
        {
            foreach (var prefix in prefixes)
            {
                var candidate = prefix + i.ToString("D7");
                if (!usedPhones.Contains(candidate))
                    return candidate;
            }
        }
        return string.Empty;
    }

    private static void Apply(ThiSinh candidate, ThiSinhImportDto dto)
    {
        candidate.MaThiSinh = dto.MaThiSinh?.Trim() ?? candidate.MaThiSinh;
        candidate.HoTen = dto.HoTen.Trim();
        candidate.NgaySinh = dto.NgaySinh;
        candidate.GioiTinh = dto.GioiTinh;
        candidate.DanToc = dto.DanToc;
        candidate.NoiSinh = dto.NoiSinh;
        candidate.QuocTich = dto.QuocTich;
        candidate.SoCccdHoChieu = dto.SoCccdHoChieu;
        candidate.SoDienThoai = dto.SoDienThoai;
        candidate.Lop = dto.Lop;
        candidate.Khoa = AcademicCatalog.NormalizeKhoa(dto.Khoa);
        candidate.NganhHoc = AcademicCatalog.NormalizeNganhHoc(candidate.Khoa, dto.NganhHoc);
        candidate.SoTien = dto.SoTien;
        candidate.EmailCaNhan = dto.EmailCaNhan;
    }

    private static string RemoveDiacritics(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
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