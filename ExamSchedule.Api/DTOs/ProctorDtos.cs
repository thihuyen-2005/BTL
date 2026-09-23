using ExamSchedule.Api.Entities;

namespace ExamSchedule.Api.DTOs;

public record ProctorListItemDto(
    int ProctorProfileId, int UserId, string StaffCode, string FullName,
    string? Department, string? Email, string? Phone, bool IsActive,
    int AssignmentCount);

public record ProctorScheduleItemDto(
    int AssignmentId, int CaThiId, string MaKyThi, string TenKyThi,
    DateTime Start, DateTime End, string MaPhong, string TenPhong,
    ProctorRole Role, ProctorAssignmentStatus Status);

public record ProctorAssignmentDto(
    int AssignmentId, int CaThiId, int ProctorProfileId, int UserId,
    string StaffCode, string FullName, string? Department,
    ProctorRole Role, ProctorAssignmentStatus Status, DateTime AssignedAt);

public record AssignProctorRequest(int CaThiId, int ProctorProfileId, ProctorRole Role);
public record ReplaceProctorRequest(int NewProctorProfileId);
public record UpdateProctorRoleRequest(ProctorRole Role);
public record CreateProctorProfileRequest(int UserId, string StaffCode, string? Department, string? Phone);

public record ExamSessionProctorSummaryDto(
    int CaThiId, string MaKyThi, string TenKyThi, DateTime Start, DateTime End,
    string MaPhong, string TenPhong, TrangThaiCaThi SessionStatus,
    int RequiredCount, int AssignedCount, List<ProctorAssignmentDto> Assignments);