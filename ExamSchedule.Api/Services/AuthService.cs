using ExamSchedule.Api.Data;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly IConfiguration _cf;

    public AuthService(AppDbContext db, JwtService jwt, IConfiguration cf)
    { _db = db; _jwt = jwt; _cf = cf; }

    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == req.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Sai tài khoản hoặc mật khẩu.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản bị khóa.");

        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var accessToken = _jwt.GenerateAccessToken(user, roles);
        var refreshToken = _jwt.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(int.Parse(_cf["Jwt:RefreshTokenDays"]!));
        await _db.SaveChangesAsync();

        return new LoginResponse(
            accessToken,
            refreshToken,
            int.Parse(_cf["Jwt:AccessTokenMinutes"]!) * 60,
            roles.FirstOrDefault() ?? "SinhVien",
            user.FullName);
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");

        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var newAccess = _jwt.GenerateAccessToken(user, roles);
        var newRefresh = _jwt.GenerateRefreshToken();

        user.RefreshToken = newRefresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(int.Parse(_cf["Jwt:RefreshTokenDays"]!));
        await _db.SaveChangesAsync();

        return new LoginResponse(
            newAccess, newRefresh,
            int.Parse(_cf["Jwt:AccessTokenMinutes"]!) * 60,
            roles.FirstOrDefault() ?? "SinhVien",
            user.FullName);
    }

    public async Task LogoutAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _db.SaveChangesAsync();
        }
    }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest req)
    {
        if (req.NewPassword != req.ConfirmPassword)
            throw new Middlewares.BusinessException("Mật khẩu xác nhận không khớp.");
        if (req.NewPassword.Length < 6)
            throw new Middlewares.BusinessException("Mật khẩu mới phải có ít nhất 6 ký tự.");

        var user = await _db.Users.FindAsync(userId)
            ?? throw new Middlewares.NotFoundException("Không tìm thấy user.");

        if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, user.PasswordHash))
            throw new Middlewares.BusinessException("Mật khẩu cũ không đúng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync();
    }
}