using ExamSchedule.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamSchedule.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<KyThi> KyThis => Set<KyThi>();
    public DbSet<CaThi> CaThis => Set<CaThi>();
    public DbSet<PhongThi> PhongThis => Set<PhongThi>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProctorProfile> ProctorProfiles => Set<ProctorProfile>();
    public DbSet<ProctorAssignment> ProctorAssignments => Set<ProctorAssignment>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasCharSet("utf8mb4");

        // ===== AppUser =====
        mb.Entity<AppUser>(e =>
        {
            e.ToTable("app_users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(50);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(100);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(100);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.RefreshToken).HasColumnName("refresh_token");
            e.Property(x => x.RefreshTokenExpiry).HasColumnName("refresh_token_expiry");
            e.Property(x => x.NgayTao).HasColumnName("created_at");
        });

        // ===== Role =====
        mb.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.RoleName).HasColumnName("role_name").HasMaxLength(50);
            e.HasIndex(x => x.RoleName).IsUnique();
        });

        // ===== UserRole =====
        mb.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
        });

        mb.Entity<ProctorProfile>(e =>
        {
            e.ToTable("proctor_profiles");
            e.HasKey(x => x.ProctorProfileId);
            e.Property(x => x.ProctorProfileId).HasColumnName("proctor_profile_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.StaffCode).HasColumnName("staff_code").HasMaxLength(50);
            e.Property(x => x.Department).HasColumnName("department").HasMaxLength(150);
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.StaffCode).IsUnique();
            e.HasOne(x => x.User).WithOne(x => x.ProctorProfile)
                .HasForeignKey<ProctorProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== PhongThi =====
        mb.Entity<PhongThi>(e =>
        {
            e.ToTable("phong_thi");
            e.HasKey(x => x.PhongThiId);
            e.Property(x => x.PhongThiId).HasColumnName("phong_thi_id");
            e.Property(x => x.MaPhong).HasColumnName("ma_phong").HasMaxLength(20);
            e.HasIndex(x => x.MaPhong).IsUnique();
            e.Property(x => x.TenPhong).HasColumnName("ten_phong").HasMaxLength(100);
            e.Property(x => x.SucChua).HasColumnName("suc_chua");
            e.Property(x => x.ViTri).HasColumnName("vi_tri").HasMaxLength(100);
        });

        // ===== KyThi =====
        mb.Entity<KyThi>(e =>
        {
            e.ToTable("ky_thi");
            e.HasKey(x => x.KyThiId);
            e.Property(x => x.KyThiId).HasColumnName("ky_thi_id");
            e.Property(x => x.MaKyThi).HasColumnName("ma_ky_thi").HasMaxLength(50);
            e.HasIndex(x => x.MaKyThi).IsUnique();
            e.Property(x => x.TenKyThi).HasColumnName("ten_ky_thi").HasMaxLength(200);
            e.Property(x => x.LoaiChungChi).HasColumnName("loai_chung_chi").HasMaxLength(50);
            e.Property(x => x.ThoiGianBatDauDk).HasColumnName("thoi_gian_bat_dau_dk");
            e.Property(x => x.ThoiGianKetThucDk).HasColumnName("thoi_gian_ket_thuc_dk");
            e.Property(x => x.TrangThai).HasColumnName("trang_thai").HasConversion<string>();
            e.Property(x => x.GhiChu).HasColumnName("ghi_chu").HasMaxLength(255);
            e.Property(x => x.NgayTao).HasColumnName("ngay_tao");
            e.Property(x => x.NgayCapNhat).HasColumnName("ngay_cap_nhat");
        });

        // ===== CaThi =====
        mb.Entity<CaThi>(e =>
        {
            e.ToTable("ca_thi");
            e.HasKey(x => x.CaThiId);
            e.Property(x => x.CaThiId).HasColumnName("ca_thi_id");
            e.Property(x => x.KyThiId).HasColumnName("ky_thi_id");
            e.Property(x => x.PhongThiId).HasColumnName("phong_thi_id");
            e.Property(x => x.ThoiGianBatDau).HasColumnName("thoi_gian_bat_dau");
            e.Property(x => x.ThoiGianKetThuc).HasColumnName("thoi_gian_ket_thuc");
            e.Property(x => x.SucChua).HasColumnName("suc_chua");
            e.Property(x => x.TrangThai).HasColumnName("trang_thai").HasConversion<string>();
            e.Property(x => x.GhiChu).HasColumnName("ghi_chu").HasMaxLength(255);
            e.Property(x => x.NgayTao).HasColumnName("ngay_tao");
            e.Property(x => x.NgayCapNhat).HasColumnName("ngay_cap_nhat");
            e.Property(x => x.RequiredProctorCount).HasColumnName("required_proctor_count").HasDefaultValue(3);

            e.HasOne(x => x.KyThi).WithMany(k => k.CaThis)
             .HasForeignKey(x => x.KyThiId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PhongThi).WithMany()
             .HasForeignKey(x => x.PhongThiId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ProctorAssignment>(e =>
        {
            e.ToTable("proctor_assignments");
            e.HasKey(x => x.ProctorAssignmentId);
            e.Property(x => x.ProctorAssignmentId).HasColumnName("proctor_assignment_id");
            e.Property(x => x.CaThiId).HasColumnName("ca_thi_id");
            e.Property(x => x.ProctorProfileId).HasColumnName("proctor_profile_id");
            e.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
            e.Property(x => x.AssignedAt).HasColumnName("assigned_at");
            e.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
            e.HasIndex(x => new { x.CaThiId, x.ProctorProfileId }).IsUnique();
            e.HasIndex(x => new { x.CaThiId, x.Role }).IsUnique();
            e.HasOne(x => x.CaThi).WithMany(x => x.ProctorAssignments)
                .HasForeignKey(x => x.CaThiId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProctorProfile).WithMany(x => x.Assignments)
                .HasForeignKey(x => x.ProctorProfileId).OnDelete(DeleteBehavior.Restrict);
        });

        // ===== AuditLog =====
        mb.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_log");
            e.HasKey(x => x.AuditId);
            e.Property(x => x.AuditId).HasColumnName("audit_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Action).HasColumnName("action").HasMaxLength(50);
            e.Property(x => x.Entity).HasColumnName("entity").HasMaxLength(50);
            e.Property(x => x.EntityId).HasColumnName("entity_id");
            e.Property(x => x.OldValue).HasColumnName("old_value");
            e.Property(x => x.NewValue).HasColumnName("new_value");
            e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}