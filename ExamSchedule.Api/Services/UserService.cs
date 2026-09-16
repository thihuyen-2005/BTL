using ExamSchedule.Api.Data;
using ExamSchedule.Api.DTOs;
using ExamSchedule.Api.Entities;
using ExamSchedule.Api.Middlewares;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public UserService(AppDbContext db, AuditService audit)
    { _db = db; _audit = audit; }

    public async Task<List<UserResponse>> GetAllAsync()
    {
        return await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.UserId)
            .Select(u => new UserResponse(
                u.UserId, u.Username, u.FullName, u.Email, u.IsActive,
                u.UserRoles.Select(ur => ur.Role.RoleName).ToList()))
            .ToListAsync();
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest req, int adminId, string ip)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
            throw new BusinessException("Username không được trống.");
        if (req.Password.Length < 6)
            throw new BusinessException("Password phải có ít nhất 6 ký tự.");
        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            throw new BusinessException("Username đã tồn tại.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == req.RoleName)
            ?? throw new BusinessException($"Role '{req.RoleName}' không tồn tại.");

        var user = new AppUser
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FullName = req.FullName,
            Email = req.Email,
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
        await _db.SaveChangesAsync();

        await _audit.LogAsync(adminId, "CREATE", "USER", user.UserId,
            null, new { user.Username, Role = role.RoleName }, ip);

        return new UserResponse(user.UserId, user.Username, user.FullName,
            user.Email, user.IsActive, new List<string> { role.RoleName });
    }

    public async Task ResetPasswordAsync(int userId, string newPassword, int adminId, string ip)
    {
        if (newPassword.Length < 6)
            throw new BusinessException("Password phải có ít nhất 6 ký tự.");

        var user = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundException("Không tìm thấy user.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(adminId, "RESET_PASSWORD", "USER", userId, null, null, ip);
    }

    public async Task ToggleActiveAsync(int userId, int adminId, string ip)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundException("Không tìm thấy user.");

        user.IsActive = !user.IsActive;
        if (!user.IsActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
        }
        await _db.SaveChangesAsync();
        await _audit.LogAsync(adminId, "TOGGLE_ACTIVE", "USER", userId,
            null, new { user.IsActive }, ip);
    }

    public async Task DeleteAsync(int userId, int adminId, string ip)
    {
        var user = await _db.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new NotFoundException("Không tìm thấy user.");

        if (user.Username == "admin")
            throw new BusinessException("Không thể xóa tài khoản admin gốc.");

        _db.UserRoles.RemoveRange(user.UserRoles);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(adminId, "DELETE", "USER", userId,
            new { user.Username }, null, ip);
    }
}