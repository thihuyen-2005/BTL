namespace ExamSchedule.Api.DTOs;

public record CreateUserRequest(
    string Username,
    string Password,
    string FullName,
    string? Email,
    string RoleName);

public record ResetPasswordRequest(string NewPassword);

public record ChangePasswordRequest(
    string OldPassword,
    string NewPassword,
    string ConfirmPassword);

public record UserResponse(
    int UserId,
    string Username,
    string FullName,
    string? Email,
    bool IsActive,
    List<string> Roles);