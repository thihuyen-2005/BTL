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
builder.Services.AddHostedService<ExamStatusBackgroundService>();
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
    opt.AddPolicy("CanViewReport", p => p.RequireRole("Admin", "QuanLy"));
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
    db.Database.Migrate();
    var existingTables = db.Database.SqlQueryRaw<string>(
        "SELECT table_name AS `Value` FROM information_schema.tables WHERE table_schema = DATABASE()")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var (oldName, newName) in new[]
    {
        ("app_users", "nguoi_dung"),
        ("roles", "vai_tro"),
        ("user_roles", "nguoi_dung_vai_tro"),
        ("audit_log", "nhat_ky")
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
            `hinh_thuc_thi` = 'TrenMay';
        UPDATE `thisinh`
        SET `khoa` = CASE
            WHEN TRIM(`khoa`) = 'Khoa GDTC - QPAN' THEN 'Khoa Giáo dục thể chất - Quốc phòng và An ninh'
            WHEN TRIM(`khoa`) IN ('Khoa KHXH & NV', 'Khoa Khoa học Xã hội và Nhân văn', 'Khoa Khoa học Xã hội & Nhân văn') THEN 'Khoa Khoa học Xã hội và Nhân văn'
            WHEN TRIM(`khoa`) IN ('Khoa KT & MT', 'Khoa Kỹ thuật và Môi trường') THEN 'Khoa Kỹ thuật và Môi trường'
            WHEN TRIM(`khoa`) IN ('Khoa KT & Du lịch', 'Khoa Kinh tế và Du lịch') THEN 'Khoa Kinh tế và Du lịch'
            WHEN TRIM(`khoa`) IN ('Khoa Ngoại ngữ') THEN 'Khoa Ngoại ngữ'
            WHEN TRIM(`khoa`) IN ('Khoa QL & Đô thị', 'Khoa Quản lý và Đô thị') THEN 'Khoa Quản lý và Đô thị'
            WHEN TRIM(`khoa`) IN ('Khoa Sư phạm', 'Khoa SP') THEN 'Khoa Sư phạm'
            WHEN TRIM(`khoa`) IN ('Khoa Toán - CNTT', 'Khoa Toán - Công nghệ thông tin') THEN 'Khoa Toán - Công nghệ thông tin'
            WHEN TRIM(`khoa`) IN ('Viện Hà Nội học và Đào tạo quốc tế', 'Vien Ha Noi hoc va Dao tao quoc te') THEN 'Viện Hà Nội học và Đào tạo quốc tế'
            ELSE TRIM(`khoa`)
        END;
        UPDATE `thisinh`
        SET `nganh_hoc` = CASE
            WHEN TRIM(`khoa`) = 'Khoa Giáo dục thể chất - Quốc phòng và An ninh' AND TRIM(`nganh_hoc`) IN ('GDTC', 'Giáo dục thể chất') THEN 'Giáo dục thể chất (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Khoa học Xã hội và Nhân văn' AND TRIM(`nganh_hoc`) IN ('Chính trị học', 'CTH') THEN 'Chính trị học (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Khoa học Xã hội và Nhân văn' AND TRIM(`nganh_hoc`) IN ('Công tác xã hội', 'CTXH') THEN 'Công tác xã hội (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Khoa học Xã hội và Nhân văn' AND TRIM(`nganh_hoc`) IN ('Luật', 'Luat') THEN 'Luật (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Khoa học Xã hội và Nhân văn' AND TRIM(`nganh_hoc`) IN ('Quản lý Giáo dục', 'QLGD') THEN 'Quản lý Giáo dục (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Khoa học Xã hội và Nhân văn' AND TRIM(`nganh_hoc`) IN ('Tâm lý học', 'Tâm lý') THEN 'Tâm lý học (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Khoa học Xã hội và Nhân văn' AND TRIM(`nganh_hoc`) IN ('Văn học', 'VH') THEN 'Văn học (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Kỹ thuật và Môi trường' AND TRIM(`nganh_hoc`) IN ('Công nghệ - Kỹ thuật môi trường', 'CNTTMT') THEN 'Công nghệ - Kỹ thuật môi trường (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Kinh tế và Du lịch' AND TRIM(`nganh_hoc`) IN ('Quản lý kinh tế', 'QLKT') THEN 'Quản lý kinh tế (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Kinh tế và Du lịch' AND TRIM(`nganh_hoc`) IN ('Quản trị dịch vụ du lịch và lữ hành', 'QTDVDL') THEN 'Quản trị dịch vụ du lịch và lữ hành (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Kinh tế và Du lịch' AND TRIM(`nganh_hoc`) IN ('Quản trị khách sạn', 'QTKS') THEN 'Quản trị khách sạn (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Kinh tế và Du lịch' AND TRIM(`nganh_hoc`) IN ('Quản trị Kinh doanh', 'QTKD') THEN 'Quản trị Kinh doanh (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Kinh tế và Du lịch' AND TRIM(`nganh_hoc`) IN ('Tài chính - Ngân hàng', 'TCNH') THEN 'Tài chính - Ngân hàng (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Ngoại ngữ' AND TRIM(`nganh_hoc`) IN ('Ngôn ngữ Anh', 'NNA') THEN 'Ngôn ngữ Anh (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Ngoại ngữ' AND TRIM(`nganh_hoc`) IN ('Ngôn ngữ Trung Quốc', 'NNTC') THEN 'Ngôn ngữ Trung Quốc (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Ngoại ngữ' AND TRIM(`nganh_hoc`) IN ('Sư phạm Tiếng Anh', 'SPTA') THEN 'Sư phạm Tiếng Anh (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Quản lý và Đô thị' AND TRIM(`nganh_hoc`) IN ('Logistics và quản lý chuỗi ứng', 'Logistics') THEN 'Logistics và quản lý chuỗi ứng (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Quản lý và Đô thị' AND TRIM(`nganh_hoc`) IN ('Quản lý công', 'QLC') THEN 'Quản lý công (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('Giáo dục Công dân', 'GDCD') THEN 'Giáo dục Công dân (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('Giáo dục đặc biệt', 'GDDB') THEN 'Giáo dục đặc biệt (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('Giáo dục Mầm non - Giáo dục hòa nhập', 'GDMN-HN') THEN 'Giáo dục Mầm non - Giáo dục hòa nhập (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('Giáo dục Mầm non', 'GDMN') THEN 'Giáo dục Mầm non (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('Giáo dục Tiểu học - Giáo dục hòa nhập', 'GDTH-HN') THEN 'Giáo dục Tiểu học - Giáo dục hòa nhập (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('Giáo dục Tiểu học', 'GDTH') THEN 'Giáo dục Tiểu học (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('Giáo dục Tiểu học tiên tiến', 'GDTHTT') THEN 'Giáo dục Tiểu học tiên tiến (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('SP Lịch sử', 'SPLS') THEN 'SP Lịch sử (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('SP Ngữ văn', 'SPNV') THEN 'SP Ngữ văn (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('SP Toán học', 'SPTOAN') THEN 'SP Toán học (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Sư phạm' AND TRIM(`nganh_hoc`) IN ('SP Vật lý', 'SPVL') THEN 'SP Vật lý (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Toán - Công nghệ thông tin' AND TRIM(`nganh_hoc`) IN ('Công nghệ Thông tin', 'CNTT') THEN 'Công nghệ Thông tin (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Toán - Công nghệ thông tin' AND TRIM(`nganh_hoc`) IN ('Sư phạm Tin học', 'SPTH') THEN 'Sư phạm Tin học (ĐH)'
            WHEN TRIM(`khoa`) = 'Khoa Toán - Công nghệ thông tin' AND TRIM(`nganh_hoc`) IN ('Toán ứng dụng', 'TUD') THEN 'Toán ứng dụng (ĐH)'
            WHEN TRIM(`khoa`) = 'Viện Hà Nội học và Đào tạo quốc tế' AND TRIM(`nganh_hoc`) IN ('Văn hóa học', 'VHH') THEN 'Văn hóa học (ĐH)'
            WHEN TRIM(`khoa`) = 'Viện Hà Nội học và Đào tạo quốc tế' AND TRIM(`nganh_hoc`) IN ('Việt Nam học', 'VNH') THEN 'Việt Nam học (ĐH)'
            ELSE TRIM(`nganh_hoc`)
        END;
        UPDATE `thisinh`
        SET `khoa` = NULLIF(TRIM(`khoa`), '')
          , `nganh_hoc` = NULLIF(TRIM(`nganh_hoc`), '');");
    var thiSinhService = scope.ServiceProvider.GetRequiredService<ThiSinhService>();
    await thiSinhService.NormalizeDataIntegrityAsync();

    // ===== FIX: MySQL không hỗ trợ CREATE INDEX IF NOT EXISTS =====
    // Phải kiểm tra information_schema.statistics trước rồi mới tạo.
    void EnsureUniqueIndex(string indexName, string tableName, string columnName)
    {
        var exists = db.Database
            .SqlQueryRaw<int>(
                @"SELECT COUNT(*) AS `Value`
                  FROM information_schema.statistics
                  WHERE table_schema = DATABASE()
                    AND table_name   = {0}
                    AND index_name   = {1}",
                tableName, indexName)
            .AsEnumerable()
            .Single() > 0;

        if (exists)
        {
            Console.WriteLine($"[Index] Bỏ qua (đã tồn tại): {indexName} trên {tableName}({columnName})");
            return;
        }

        // Identifier không tham số hoá được trong MySQL, nhưng ở đây là hằng số nội bộ nên an toàn.
        db.Database.ExecuteSqlRaw(
            $"CREATE UNIQUE INDEX `{indexName}` ON `{tableName}` (`{columnName}`);");
        Console.WriteLine($"[Index] Đã tạo: {indexName} trên {tableName}({columnName})");
    }

    EnsureUniqueIndex("IX_thisinh_so_dien_thoai", "thisinh", "so_dien_thoai");
    EnsureUniqueIndex("IX_thisinh_ma_thisinh",   "thisinh", "ma_thisinh");
    // ===== END FIX =====

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
    var requiredRoles = new[] { "Admin", "CBKT", "QuanLy", "SinhVien" };
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

    // Seed 15 hồ sơ giám thị mới với thông tin cụ thể, đồng bộ với bảng giam_thi.
    var proctorSeed = new[]
    {
        new { StaffCode = "GT-001", FullName = "Nguyễn Văn Hoàng", Email = "nguyenvanhoang@khaothi.edu.vn", Phone = "0903123456", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-002", FullName = "Trần Thị Lan", Email = "tranthilan@khaothi.edu.vn", Phone = "0903456789", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-003", FullName = "Lê Minh Tuấn", Email = "leminhtuan@khaothi.edu.vn", Phone = "0903789123", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-004", FullName = "Phạm Thị Hương", Email = "phamthihuong@khaothi.edu.vn", Phone = "0903123987", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-005", FullName = "Hoàng Minh Quân", Email = "hoangminhquan@khaothi.edu.vn", Phone = "0903876543", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-006", FullName = "Đỗ Thị Nhàn", Email = "dothinhan@khaothi.edu.vn", Phone = "0903567891", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-007", FullName = "Nguyễn Thị Mai", Email = "nguyenthimai@khaothi.edu.vn", Phone = "0903234567", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-008", FullName = "Trần Quang Huy", Email = "tranquanghuy@khaothi.edu.vn", Phone = "0903345678", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-009", FullName = "Võ Thị Thúy", Email = "vothithuy@khaothi.edu.vn", Phone = "0903987654", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-010", FullName = "Bùi Đức Anh", Email = "buiducanh@khaothi.edu.vn", Phone = "0903890123", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-011", FullName = "Nguyễn Thành Tuấn", Email = "nguyenthanhtuan@khaothi.edu.vn", Phone = "0903123458", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-012", FullName = "Lê Thị Huyền", Email = "lethihuyen@khaothi.edu.vn", Phone = "0903765432", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-013", FullName = "Phạm Văn Dũng", Email = "phamvandung@khaothi.edu.vn", Phone = "0903654321", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-014", FullName = "Trần Thị Uyên", Email = "tranthuyen@khaothi.edu.vn", Phone = "0903543210", Department = "Phòng Khảo thí" },
        new { StaffCode = "GT-015", FullName = "Hoàng Văn Sơn", Email = "hoangvanson@khaothi.edu.vn", Phone = "0903987650", Department = "Phòng Khảo thí" }
    };

    foreach (var proctor in proctorSeed)
    {
        if (db.ProctorProfiles.Any(p => p.StaffCode == proctor.StaffCode))
            continue;

        db.ProctorProfiles.Add(new ProctorProfile
        {
            StaffCode = proctor.StaffCode,
            FullName = proctor.FullName,
            Email = proctor.Email,
            Phone = proctor.Phone,
            Department = proctor.Department,
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