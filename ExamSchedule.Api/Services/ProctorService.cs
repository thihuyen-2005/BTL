using ExamSchedule.Api.Data;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Entities;
using ExamSchedule.Api.Middlewares;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Services;

public class ProctorService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public ProctorService(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<List<ProctorListItemDto>> GetProctorsAsync(
        string? keyword, string? status, int? caThiId)
    {
        var query = _db.ProctorProfiles.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Assignments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(p => p.StaffCode.Contains(value)
                || p.User.FullName.Contains(value)
                || (p.Department != null && p.Department.Contains(value)));
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
            query = query.Where(p => p.IsActive && p.User.IsActive);
        else if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
            query = query.Where(p => !p.IsActive || !p.User.IsActive);
        else if (string.Equals(status, "free", StringComparison.OrdinalIgnoreCase))
            query = query.Where(p => p.IsActive && p.User.IsActive
                && !p.Assignments.Any(a => a.Status == ProctorAssignmentStatus.Da_phan_cong));
        else if (string.Equals(status, "busy", StringComparison.OrdinalIgnoreCase))
            query = query.Where(p => p.Assignments.Any(a => a.Status == ProctorAssignmentStatus.Da_phan_cong));

        if (caThiId.HasValue)
            query = query.Where(p => !p.Assignments.Any(a => a.CaThiId == caThiId.Value
                && a.Status == ProctorAssignmentStatus.Da_phan_cong));

        return await query.OrderBy(p => p.StaffCode)
            .Select(p => new ProctorListItemDto(
                p.ProctorProfileId, p.UserId, p.StaffCode, p.User.FullName,
                p.Department, p.User.Email, p.Phone, p.IsActive && p.User.IsActive,
                p.Assignments.Count(a => a.Status == ProctorAssignmentStatus.Da_phan_cong)))
            .ToListAsync();
    }

    public async Task<List<ProctorScheduleItemDto>> GetScheduleAsync(int profileId, int? currentUserId)
    {
        var profile = await _db.ProctorProfiles.AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.ProctorProfileId == profileId)
            ?? throw new NotFoundException("Không tìm thấy giám thị.");

        if (currentUserId.HasValue && profile.UserId != currentUserId.Value)
            throw new BusinessException("Bạn chỉ được xem lịch coi thi của chính mình.");

        return await _db.ProctorAssignments.AsNoTracking()
            .Where(a => a.ProctorProfileId == profileId
                && a.Status == ProctorAssignmentStatus.Da_phan_cong)
            .OrderBy(a => a.CaThi.ThoiGianBatDau)
            .Select(a => new ProctorScheduleItemDto(
                a.ProctorAssignmentId, a.CaThiId, a.CaThi.KyThi.MaKyThi,
                a.CaThi.KyThi.TenKyThi, a.CaThi.ThoiGianBatDau,
                a.CaThi.ThoiGianKetThuc, a.CaThi.PhongThi.MaPhong,
                a.CaThi.PhongThi.TenPhong, a.Role, a.Status))
            .ToListAsync();
    }

    public async Task<ExamSessionProctorSummaryDto> GetSessionAssignmentsAsync(int caThiId)
    {
        var session = await _db.CaThis.AsNoTracking()
            .Include(c => c.KyThi).Include(c => c.PhongThi)
            .FirstOrDefaultAsync(c => c.CaThiId == caThiId)
            ?? throw new NotFoundException("Ca thi không tồn tại.");

        var assignments = (await _db.ProctorAssignments.AsNoTracking()
            .Include(a => a.ProctorProfile).ThenInclude(p => p.User)
            .Where(a => a.CaThiId == caThiId && a.Status == ProctorAssignmentStatus.Da_phan_cong)
            .OrderBy(a => a.Role)
            .ToListAsync()).Select(ToDto).ToList();

        return new ExamSessionProctorSummaryDto(
            session.CaThiId, session.KyThi.MaKyThi, session.KyThi.TenKyThi,
            session.ThoiGianBatDau, session.ThoiGianKetThuc,
            session.PhongThi.MaPhong, session.PhongThi.TenPhong,
            session.TrangThai, session.RequiredProctorCount,
            assignments.Count, assignments);
    }

    public async Task<ProctorAssignmentDto> GetAssignmentAsync(int assignmentId)
    {
        var assignment = await _db.ProctorAssignments.AsNoTracking()
            .Include(a => a.ProctorProfile).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.ProctorAssignmentId == assignmentId)
            ?? throw new NotFoundException("Phân công không tồn tại.");
        return ToDto(assignment);
    }

    public async Task<ProctorAssignmentDto> AssignAsync(
        AssignProctorRequest request, int actorId, string ip)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        var session = await GetAssignableSessionAsync(request.CaThiId);
        var profile = await GetAssignableProfileAsync(request.ProctorProfileId);

        await ValidateAssignmentAsync(session, profile, request.Role, null);

        var assignment = new ProctorAssignment
        {
            CaThiId = session.CaThiId,
            ProctorProfileId = profile.ProctorProfileId,
            Role = request.Role,
            Status = ProctorAssignmentStatus.Da_phan_cong,
            AssignedAt = DateTime.UtcNow
        };
        _db.ProctorAssignments.Add(assignment);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorId, "ASSIGN_PROCTOR", "PROCTOR_ASSIGNMENT",
            assignment.ProctorAssignmentId, null, assignment, ip);
        await transaction.CommitAsync();

        await _db.Entry(assignment).Reference(a => a.ProctorProfile).LoadAsync();
        await _db.Entry(assignment.ProctorProfile).Reference(p => p.User).LoadAsync();
        return ToDto(assignment);
    }

    public async Task UnassignAsync(int assignmentId, int actorId, string ip)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        var assignment = await _db.ProctorAssignments
            .Include(a => a.CaThi)
            .Include(a => a.ProctorProfile).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.ProctorAssignmentId == assignmentId)
            ?? throw new NotFoundException("Phân công không tồn tại.");

        EnsureSessionCanChange(assignment.CaThi);
        var old = ToDto(assignment);
        _db.ProctorAssignments.Remove(assignment);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorId, "UNASSIGN_PROCTOR", "PROCTOR_ASSIGNMENT",
            assignmentId, old, null, ip);
        await transaction.CommitAsync();
    }

    public async Task<ProctorAssignmentDto> ReplaceAsync(
        int assignmentId, ReplaceProctorRequest request, int actorId, string ip)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        var oldAssignment = await _db.ProctorAssignments
            .Include(a => a.CaThi)
            .Include(a => a.ProctorProfile).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.ProctorAssignmentId == assignmentId)
            ?? throw new NotFoundException("Phân công không tồn tại.");
        EnsureSessionCanChange(oldAssignment.CaThi);

        var profile = await GetAssignableProfileAsync(request.NewProctorProfileId);
        await ValidateAssignmentAsync(oldAssignment.CaThi, profile, oldAssignment.Role, assignmentId);

        var old = ToDto(oldAssignment);
        oldAssignment.ProctorProfileId = profile.ProctorProfileId;
        oldAssignment.AssignedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorId, "REPLACE_PROCTOR", "PROCTOR_ASSIGNMENT",
            assignmentId, old, oldAssignment, ip);
        await transaction.CommitAsync();

        await _db.Entry(oldAssignment).Reference(a => a.ProctorProfile).LoadAsync();
        await _db.Entry(oldAssignment.ProctorProfile).Reference(p => p.User).LoadAsync();
        return ToDto(oldAssignment);
    }

    public async Task<ProctorAssignmentDto> UpdateRoleAsync(
        int assignmentId, UpdateProctorRoleRequest request, int actorId, string ip)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        var assignment = await _db.ProctorAssignments
            .Include(a => a.CaThi)
            .Include(a => a.ProctorProfile).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.ProctorAssignmentId == assignmentId)
            ?? throw new NotFoundException("Phân công không tồn tại.");
        EnsureSessionCanChange(assignment.CaThi);

        if (request.Role == ProctorRole.Truong_ca && await _db.ProctorAssignments.AnyAsync(a =>
            a.CaThiId == assignment.CaThiId && a.ProctorAssignmentId != assignmentId
            && a.Status == ProctorAssignmentStatus.Da_phan_cong && a.Role == ProctorRole.Truong_ca))
            throw new ConflictException("Ca thi này đã có Trưởng ca.");

        var old = ToDto(assignment);
        assignment.Role = request.Role;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorId, "UPDATE_PROCTOR_ROLE", "PROCTOR_ASSIGNMENT",
            assignmentId, old, assignment, ip);
        await transaction.CommitAsync();
        return ToDto(assignment);
    }

    public async Task<List<ProctorAssignmentDto>> AutoAssignAsync(
        int caThiId, int actorId, string ip)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        var session = await GetAssignableSessionAsync(caThiId);
        var existingRoles = await _db.ProctorAssignments
            .Where(a => a.CaThiId == caThiId && a.Status == ProctorAssignmentStatus.Da_phan_cong)
            .Select(a => a.Role).ToListAsync();
        var roles = Enum.GetValues<ProctorRole>()
            .Where(role => !existingRoles.Contains(role))
            .Take(Math.Max(0, session.RequiredProctorCount - existingRoles.Count))
            .ToList();

        var candidates = await _db.ProctorProfiles
            .Include(p => p.User).Include(p => p.Assignments)
            .Where(p => p.IsActive && p.User.IsActive
                && !p.Assignments.Any(a => a.CaThiId == caThiId
                    && a.Status == ProctorAssignmentStatus.Da_phan_cong)
                && !p.Assignments.Any(a => a.Status == ProctorAssignmentStatus.Da_phan_cong
                    && session.ThoiGianBatDau < a.CaThi.ThoiGianKetThuc
                    && session.ThoiGianKetThuc > a.CaThi.ThoiGianBatDau))
            .OrderBy(p => p.Assignments.Count(a => a.Status == ProctorAssignmentStatus.Da_phan_cong))
            .ThenBy(p => p.StaffCode)
            .Take(roles.Count).ToListAsync();

        if (candidates.Count < roles.Count)
            throw new ConflictException($"Không đủ giám thị phù hợp. Còn thiếu {roles.Count - candidates.Count} người.");

        var added = new List<ProctorAssignment>();
        for (var i = 0; i < roles.Count; i++)
        {
            var assignment = new ProctorAssignment
            {
                CaThiId = caThiId, ProctorProfileId = candidates[i].ProctorProfileId,
                Role = roles[i], AssignedAt = DateTime.UtcNow
            };
            _db.ProctorAssignments.Add(assignment);
            added.Add(assignment);
        }
        await _db.SaveChangesAsync();
        foreach (var assignment in added)
            await _audit.LogAsync(actorId, "AUTO_ASSIGN_PROCTOR", "PROCTOR_ASSIGNMENT",
                assignment.ProctorAssignmentId, null, assignment, ip);
        await transaction.CommitAsync();

        var result = await _db.ProctorAssignments.AsNoTracking()
            .Include(a => a.ProctorProfile).ThenInclude(p => p.User)
            .Where(a => added.Select(x => x.ProctorAssignmentId).Contains(a.ProctorAssignmentId))
            .ToListAsync();
        return result.Select(ToDto).ToList();
    }

    public async Task<ProctorListItemDto> CreateProfileAsync(
        CreateProctorProfileRequest request, int actorId, string ip)
    {
        if (string.IsNullOrWhiteSpace(request.StaffCode))
            throw new BusinessException("Mã cán bộ không được trống.");
        var user = await _db.Users.FindAsync(request.UserId)
            ?? throw new NotFoundException("Cán bộ không tồn tại trong hệ thống.");
        if (await _db.ProctorProfiles.AnyAsync(p => p.UserId == request.UserId))
            throw new BusinessException("Cán bộ đã có hồ sơ giám thị.");
        if (await _db.ProctorProfiles.AnyAsync(p => p.StaffCode == request.StaffCode.Trim()))
            throw new BusinessException("Mã cán bộ đã tồn tại.");

        var profile = new ProctorProfile
        {
            UserId = user.UserId, StaffCode = request.StaffCode.Trim(),
            Department = request.Department?.Trim(), Phone = request.Phone?.Trim(),
            IsActive = user.IsActive
        };
        _db.ProctorProfiles.Add(profile);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorId, "CREATE_PROCTOR", "PROCTOR_PROFILE",
            profile.ProctorProfileId, null, profile, ip);
        return new ProctorListItemDto(profile.ProctorProfileId, user.UserId,
            profile.StaffCode, user.FullName, profile.Department, user.Email,
            profile.Phone, profile.IsActive && user.IsActive, 0);
    }

    private async Task<CaThi> GetAssignableSessionAsync(int caThiId)
    {
        var session = await _db.CaThis.FirstOrDefaultAsync(c => c.CaThiId == caThiId)
            ?? throw new NotFoundException("Ca thi không tồn tại.");
        EnsureSessionCanChange(session);
        return session;
    }

    private async Task<ProctorProfile> GetAssignableProfileAsync(int profileId)
    {
        var profile = await _db.ProctorProfiles.Include(p => p.User)
            .FirstOrDefaultAsync(p => p.ProctorProfileId == profileId)
            ?? throw new NotFoundException("Cán bộ không tồn tại hoặc chưa có hồ sơ giám thị.");
        if (!profile.IsActive || !profile.User.IsActive)
            throw new BusinessException("Không thể phân công cán bộ không hoạt động.");
        return profile;
    }

    private async Task ValidateAssignmentAsync(
        CaThi session, ProctorProfile profile, ProctorRole role, int? ignoredAssignmentId)
    {
        if (await _db.ProctorAssignments.AnyAsync(a => a.CaThiId == session.CaThiId
            && a.ProctorProfileId == profile.ProctorProfileId
            && a.Status == ProctorAssignmentStatus.Da_phan_cong
            && a.ProctorAssignmentId != ignoredAssignmentId))
            throw new ConflictException("Cán bộ này đã được phân công trong ca thi.");

        if (role == ProctorRole.Truong_ca && await _db.ProctorAssignments.AnyAsync(a =>
            a.CaThiId == session.CaThiId && a.Role == ProctorRole.Truong_ca
            && a.Status == ProctorAssignmentStatus.Da_phan_cong
            && a.ProctorAssignmentId != ignoredAssignmentId))
            throw new ConflictException("Ca thi này đã có Trưởng ca.");

        var conflict = await _db.ProctorAssignments
            .Include(a => a.CaThi)
            .Where(a => a.ProctorProfileId == profile.ProctorProfileId
                && a.Status == ProctorAssignmentStatus.Da_phan_cong
                && a.ProctorAssignmentId != ignoredAssignmentId
                && session.ThoiGianBatDau < a.CaThi.ThoiGianKetThuc
                && session.ThoiGianKetThuc > a.CaThi.ThoiGianBatDau)
            .Select(a => new { a.CaThi.ThoiGianBatDau, a.CaThi.ThoiGianKetThuc })
            .FirstOrDefaultAsync();
        if (conflict != null)
            throw new ConflictException(
                $"Giám thị {profile.User.FullName} đã có lịch coi thi từ " +
                $"{conflict.ThoiGianBatDau:HH:mm dd/MM/yyyy} đến " +
                $"{conflict.ThoiGianKetThuc:HH:mm dd/MM/yyyy}. Không thể phân công vào ca mới.");
    }

    private static void EnsureSessionCanChange(CaThi session)
    {
        if (session.TrangThai == TrangThaiCaThi.Dong)
            throw new BusinessException("Không thể thay đổi phân công cho ca thi đã đóng.");
        if (session.TrangThai == TrangThaiCaThi.Huy)
            throw new BusinessException("Không thể thay đổi phân công cho ca thi đã hủy.");
        if (session.ThoiGianBatDau <= DateTime.Now)
            throw new BusinessException("Không thể thay đổi phân công khi ca thi đã bắt đầu.");
    }

    private static ProctorAssignmentDto ToDto(ProctorAssignment a) =>
        new(a.ProctorAssignmentId, a.CaThiId, a.ProctorProfileId,
            a.ProctorProfile.UserId, a.ProctorProfile.StaffCode,
            a.ProctorProfile.User.FullName, a.ProctorProfile.Department,
            a.Role, a.Status, a.AssignedAt);
}