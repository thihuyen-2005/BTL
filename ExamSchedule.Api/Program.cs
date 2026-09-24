using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using ExamSchedule.Api.Data;
using ExamSchedule.Api.Entities;
using ExamSchedule.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var enableHttpsRedirection = builder.Configuration.GetValue(
    "HttpsRedirection:Enabled",
    !builder.Environment.IsDevelopment());

if (builder.Environment.IsDevelopment())
    builder.WebHost.UseUrls("http://0.0.0.0:5000");

// ===== Kết nối MySQL =====
var connStr = builder.Configuration.GetConnectionString("Default")!;
if (!connStr.Contains("CharSet=", StringComparison.OrdinalIgnoreCase))
    connStr += ";CharSet=utf8mb4";
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

builder.Services.AddCors();

// ===== Đăng ký services =====
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExamService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ExamSessionService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ExamStatusUpdater>();
builder.Services.AddScoped<ProctorService>();
builder.Services.AddScoped<ThiSinhService>();
builder.Services.AddScoped<XepLichService>();

// ===== Cấu hình JWT =====
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// ===== Policy phân quyền =====
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("IsAdmin",       p => p.RequireRole("Admin"));
    opt.AddPolicy("CanManageExam", p => p.RequireRole("Admin", "CBKT"));
    opt.AddPolicy("CanViewExam",   p => p.RequireRole("Admin", "CBKT", "QuanLy"));
    opt.AddPolicy("CanApprove",    p => p.RequireRole("Admin", "QuanLy"));
    opt.AddPolicy("CanViewReport", p => p.RequireRole("Admin", "QuanLy", "KeToan"));
    opt.AddPolicy("IsStudent",     p => p.RequireRole("SinhVien"));
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();

// ===== Swagger + nút Authorize =====
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Nhập 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExamSchedule.Api.Middlewares.ExceptionMiddleware>();

// ===== Seed Roles + Users (chạy 1 lần) =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // The legacy database may have been imported from SQL without EF history.
    // Baseline it before applying only the new migrations.
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
            `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
            `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
            PRIMARY KEY (`MigrationId`)
        ) CHARACTER SET=utf8mb4;
        INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
        SELECT '20260914043534_InitialCreate', '8.0.0'
        WHERE EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = DATABASE() AND table_name = 'app_users'
        );");
    var legacyProctorSchemaExists = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS `Value` FROM information_schema.tables
        WHERE table_schema = DATABASE() AND table_name = 'giam_thi'")
        .Single() > 0;
    if (legacyProctorSchemaExists)
    {
        db.Database.ExecuteSqlRaw(@"
            DELETE FROM `__EFMigrationsHistory`
            WHERE `MigrationId` IN (
                '20260923120000_AddProctorManagement',
                '20260923133609_AddProctorManagement'
            );
            INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('20260923133609_AddProctorManagementGenerated', '8.0.0');");
    }
    db.Database.Migrate();
    var existingTables = db.Database.SqlQueryRaw<string>(
        "SELECT table_name AS `Value` FROM information_schema.tables WHERE table_schema = DATABASE()")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var (oldName, newName) in new[]
    {
        ("app_users", "nguoi_dung"),
        ("roles", "vai_tro"),
        ("user_roles", "nguoi_dung_vai_tro"),
        ("audit_log", "nhat_ky"),
        ("proctor_profiles", "giam_thi_cu"),
        ("proctor_assignments", "phan_cong_giam_thi_cu")
    })
    {
        var oldExists = existingTables.Contains(oldName);
        var newExists = existingTables.Contains(newName);
        if (oldExists && !newExists)
        {
            var renameSql = (oldName, newName) switch
            {
                ("app_users", "nguoi_dung") => "RENAME TABLE `app_users` TO `nguoi_dung`;",
                ("roles", "vai_tro") => "RENAME TABLE `roles` TO `vai_tro`;",
                ("user_roles", "nguoi_dung_vai_tro") => "RENAME TABLE `user_roles` TO `nguoi_dung_vai_tro`;",
                ("audit_log", "nhat_ky") => "RENAME TABLE `audit_log` TO `nhat_ky`;",
                ("proctor_profiles", "giam_thi_cu") => "RENAME TABLE `proctor_profiles` TO `giam_thi_cu`;",
                ("proctor_assignments", "phan_cong_giam_thi_cu") => "RENAME TABLE `proctor_assignments` TO `phan_cong_giam_thi_cu`;",
                _ => throw new InvalidOperationException("Tên bảng rename không hợp lệ.")
            };
            db.Database.ExecuteSqlRaw(renameSql);
        }
    }
    db.Database.ExecuteSqlRaw(@"
        UPDATE `ky_thi`
        SET `ma_ky_thi` = CASE
            WHEN `ma_ky_thi` LIKE 'MOS%' THEN CONCAT('NLS-', REPLACE(`ma_ky_thi`, 'MOS - ', ''))
            WHEN `ma_ky_thi` LIKE 'ICDL%' THEN CONCAT('NLS-', REPLACE(REPLACE(`ma_ky_thi`, 'ICDL-', ''), 'ICDL - ', ''))
            ELSE `ma_ky_thi`
        END;
        UPDATE `ky_thi`
        SET `loai_chung_chi` = 'NangLucSo',
            `ten_ky_thi` = CONCAT('Kỳ thi Năng lực số - ', `ma_ky_thi`);
        UPDATE `ca_thi`
        SET `thoi_gian_ket_thuc` = DATE_ADD(`thoi_gian_bat_dau`, INTERVAL 120 MINUTE),
            `hinh_thuc_thi` = 'TrenMay';");
    var requiredColumnExists = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS `Value` FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'ca_thi'
          AND column_name = 'required_proctor_count'").Single() > 0;
    if (!requiredColumnExists)
    {
        db.Database.ExecuteSqlRaw(@"
            ALTER TABLE `ca_thi`
                ADD COLUMN `required_proctor_count` int NOT NULL DEFAULT 3;");
    }
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS `giam_thi` (
            `giam_thi_id` int NOT NULL AUTO_INCREMENT,
            `ma_giam_thi` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
            `ho_ten` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
            `email` varchar(100) CHARACTER SET utf8mb4 NULL,
            `so_dien_thoai` varchar(20) CHARACTER SET utf8mb4 NULL,
            `don_vi` varchar(150) CHARACTER SET utf8mb4 NULL,
            `trang_thai` tinyint(1) NOT NULL DEFAULT 1,
            `ngay_tao` datetime(6) NOT NULL,
            `updated_at` datetime(6) NULL,
            PRIMARY KEY (`giam_thi_id`),
            UNIQUE KEY `IX_giam_thi_ma_giam_thi` (`ma_giam_thi`)
        ) CHARACTER SET=utf8mb4;
        CREATE TABLE IF NOT EXISTS `giam_thi_phan_cong` (
            `phan_cong_id` int NOT NULL AUTO_INCREMENT,
            `ca_thi_id` int NOT NULL,
            `giam_thi_id` int NOT NULL,
            `vai_tro` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
            `trang_thai` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
            `ngay_phan_cong` datetime(6) NOT NULL,
            `ngay_huy` datetime(6) NULL,
            PRIMARY KEY (`phan_cong_id`),
            UNIQUE KEY `IX_giam_thi_phan_cong_ca_thi_id_giam_thi_id` (`ca_thi_id`, `giam_thi_id`),
            UNIQUE KEY `IX_giam_thi_phan_cong_ca_thi_id_vai_tro` (`ca_thi_id`, `vai_tro`),
            KEY `IX_giam_thi_phan_cong_ca_thi_id` (`ca_thi_id`),
            KEY `IX_giam_thi_phan_cong_giam_thi_id` (`giam_thi_id`),
            CONSTRAINT `FK_giam_thi_phan_cong_ca_thi_ca_thi_id`
                FOREIGN KEY (`ca_thi_id`) REFERENCES `ca_thi` (`ca_thi_id`) ON DELETE RESTRICT,
            CONSTRAINT `FK_giam_thi_phan_cong_giam_thi_giam_thi_id`
                FOREIGN KEY (`giam_thi_id`) REFERENCES `giam_thi` (`giam_thi_id`) ON DELETE RESTRICT
        ) CHARACTER SET=utf8mb4;
        INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
        VALUES ('20260923133609_AddProctorManagementGenerated', '8.0.0');");

    // 1) Đảm bảo đủ role
    var requiredRoles = new[] { "Admin", "CBKT", "QuanLy", "KeToan", "SinhVien" };
    foreach (var roleName in requiredRoles)
    {
        if (!db.Roles.Any(r => r.RoleName == roleName))
            db.Roles.Add(new Role { RoleName = roleName });
    }
    db.SaveChanges();

    // 2) Seed từng user nếu chưa tồn tại
    var adminSeeds = new[]
    {
        (Username: "admin",    Password: "Admin@2026",  FullName: "Quản trị viên",     Email: "admin@example.com",   Role: "Admin"),
        (Username: "cbkt01",   Password: "Cbkt@2026",   FullName: "Nguyễn Văn Cường",   Email: "cbkt@example.com",    Role: "CBKT"),
        (Username: "quanly01", Password: "Quanly@2026", FullName: "Trần Thị Dương",  Email: "quanly@example.com",  Role: "QuanLy"),
        (Username: "ketoan01", Password: "Ketoan@2026", FullName: "Lê Thu Hường",    Email: "ketoan@example.com",  Role: "KeToan"),
    };

    foreach (var s in adminSeeds)
    {
        if (db.Users.Any(u => u.Username == s.Username))
        {
            Console.WriteLine($">>> Skipped: {s.Username} (đã tồn tại)");
            continue;
        }

        var user = new AppUser
        {
            Username = s.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(s.Password),
            FullName = s.FullName,
            Email = s.Email
        };
        db.Users.Add(user);
        db.SaveChanges();

        var role = db.Roles.First(r => r.RoleName == s.Role);
        db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
        db.SaveChanges();

        Console.WriteLine($">>> Created: {s.Username,-10} | {s.Password,-14} | {s.Role}");
    }

    // Seed hồ sơ giám thị từ các quản lý hiện có, nhưng không phụ thuộc vào bảng app_users.
    var proctorUsers = db.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .Where(u => u.IsActive && u.UserRoles.Any(ur =>
            new[] { "Admin", "CBKT", "QuanLy" }.Contains(ur.Role.RoleName)))
        .ToList();
    foreach (var user in proctorUsers)
    {
        if (db.ProctorProfiles.Any(p => p.StaffCode == $"GT-{user.UserId:000}"))
            continue;

        db.ProctorProfiles.Add(new ProctorProfile
        {
            StaffCode = $"GT-{user.UserId:000}",
            FullName = user.FullName,
            Email = user.Email,
            Department = "Phòng Khảo thí",
            IsActive = true
        });
    }
    db.SaveChanges();
}

app.UseSwagger();
app.UseSwaggerUI();

if (enableHttpsRedirection)
    app.UseHttpsRedirection();
app.UseCors(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    service = "ExamSchedule.Api",
    status = "ok",
    swagger = "/swagger/index.html"
}));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();