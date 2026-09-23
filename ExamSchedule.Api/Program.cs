using System.Text;
using System.Text.Encodings.Web;
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
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();