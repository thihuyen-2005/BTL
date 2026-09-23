namespace ExamSchedule.Api.DTOs;

public record LoginRequest(string Username, string Password);

public record RefreshRequest(string RefreshToken);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string Role,
    string FullName);

public record CurrentUserResponse(
    int UserId,
    string Username,
    string Role,
    string FullName);