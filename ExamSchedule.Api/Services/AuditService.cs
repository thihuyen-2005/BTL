using System.Text.Json;
using System.Text.Json.Serialization;
using ExamSchedule.Api.Data;
using ExamSchedule.Api.Entities;

namespace ExamSchedule.Api.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    // Cấu hình để bỏ qua cycle khi serialize
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public AuditService(AppDbContext db) { _db = db; }

    public async Task LogAsync(int? userId, string action, string entity,
                                int entityId, object? oldVal, object? newVal, string ip)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            OldValue = oldVal is null ? null : JsonSerializer.Serialize(oldVal, _jsonOpts),
            NewValue = newVal is null ? null : JsonSerializer.Serialize(newVal, _jsonOpts),
            IpAddress = ip
        });
        await _db.SaveChangesAsync();
    }
}