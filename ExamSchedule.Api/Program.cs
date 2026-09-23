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
        WHERE table_schema = DATABASE() AND table_name = 'proctor_profiles'")
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
        CREATE TABLE IF NOT EXISTS `proctor_profiles` (
            `proctor_profile_id` int NOT NULL AUTO_INCREMENT,
            `user_id` int NOT NULL,
            `staff_code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
            `department` varchar(150) CHARACTER SET utf8mb4 NULL,
            `phone` varchar(30) CHARACTER SET utf8mb4 NULL,
            `is_active` tinyint(1) NOT NULL,
            `created_at` datetime(6) NOT NULL,
            `updated_at` datetime(6) NULL,
            PRIMARY KEY (`proctor_profile_id`),
            UNIQUE KEY `IX_proctor_profiles_user_id` (`user_id`),
            UNIQUE KEY `IX_proctor_profiles_staff_code` (`staff_code`),
            CONSTRAINT `FK_proctor_profiles_app_users_user_id`
                FOREIGN KEY (`user_id`) REFERENCES `app_users` (`user_id`) ON DELETE RESTRICT
        ) CHARACTER SET=utf8mb4;
        CREATE TABLE IF NOT EXISTS `proctor_assignments` (
            `proctor_assignment_id` int NOT NULL AUTO_INCREMENT,
            `ca_thi_id` int NOT NULL,
            `proctor_profile_id` int NOT NULL,
            `role` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
            `status` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
            `assigned_at` datetime(6) NOT NULL,
            `cancelled_at` datetime(6) NULL,
            PRIMARY KEY (`proctor_assignment_id`),
            UNIQUE KEY `IX_proctor_assignments_ca_thi_id_proctor_profile_id`
                (`ca_thi_id`, `proctor_profile_id`),
            UNIQUE KEY `IX_proctor_assignments_ca_thi_id_role` (`ca_thi_id`, `role`),
            KEY `IX_proctor_assignments_ca_thi_id` (`ca_thi_id`),
            KEY `IX_proctor_assignments_proctor_profile_id` (`proctor_profile_id`),
            CONSTRAINT `FK_proctor_assignments_ca_thi_ca_thi_id`
                FOREIGN KEY (`ca_thi_id`) REFERENCES `ca_thi` (`ca_thi_id`) ON DELETE RESTRICT,
            CONSTRAINT `FK_proctor_assignments_proctor_profiles_proctor_profile_id`
                FOREIGN KEY (`proctor_profile_id`) REFERENCES `proctor_profiles` (`proctor_profile_id`) ON DELETE RESTRICT
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

    // Existing users are the source of truth for proctor profiles.
    var proctorUsers = db.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .Where(u => u.IsActive && u.UserRoles.Any(ur =>
            new[] { "Admin", "CBKT", "QuanLy" }.Contains(ur.Role.RoleName)))
        .ToList();
    foreach (var user in proctorUsers)
    {
        if (db.ProctorProfiles.Any(p => p.UserId == user.UserId))
            continue;

        db.ProctorProfiles.Add(new ProctorProfile
        {
            UserId = user.UserId,
            StaffCode = $"CB-{user.UserId:000}",
            Department = "Phòng Khảo thí",
            IsActive = true
        });
    }
    db.SaveChanges();
}

app.UseSwagger();
app.UseSwaggerUI();

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