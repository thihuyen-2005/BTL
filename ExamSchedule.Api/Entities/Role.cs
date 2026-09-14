namespace ExamSchedule.Api.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole
{
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}