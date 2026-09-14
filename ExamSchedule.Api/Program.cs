using System.Text;
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
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

builder.Services.AddCors();                    // ⬅️ THÊM

// ===== Đăng ký services =====
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExamService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ExamSessionService>();

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
    opt.AddPolicy("CanManageExam", p => p.RequireRole("Admin", "CBKT"));
    opt.AddPolicy("CanViewExam", p => p.RequireRole("Admin", "CBKT", "QuanLy", "SinhVien"));
});

builder.Services.AddControllers();
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

// ===== Seed user admin + cbkt (chạy 1 lần) =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Users.Any())
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("123456");

        var admin = new AppUser
        {
            Username = "admin",
            PasswordHash = hash,
            FullName = "Quản trị viên",
            Email = "admin@example.com"
        };
        db.Users.Add(admin);

        var cbkt = new AppUser
        {
            Username = "cbkt01",
            PasswordHash = hash,
            FullName = "Nguyễn Văn CBKT",
            Email = "cbkt@example.com"
        };
        db.Users.Add(cbkt);
        db.SaveChanges();

        var roleAdmin = db.Roles.First(r => r.RoleName == "Admin");
        var roleCbkt = db.Roles.First(r => r.RoleName == "CBKT");

        db.UserRoles.Add(new UserRole { UserId = admin.UserId, RoleId = roleAdmin.RoleId });
        db.UserRoles.Add(new UserRole { UserId = cbkt.UserId, RoleId = roleCbkt.RoleId });
        db.SaveChanges();

        Console.WriteLine(">>> Đã seed user admin và cbkt với password 123456");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());   // ⬅️ THÊM
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();