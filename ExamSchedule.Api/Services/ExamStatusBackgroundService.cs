namespace ExamSchedule.Api.Services;

public class ExamStatusBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExamStatusBackgroundService> _logger;

    public ExamStatusBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExamStatusBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var updater = scope.ServiceProvider.GetRequiredService<ExamStatusUpdater>();
                await updater.UpdateAllAsync();
            }
            catch (Exception error) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(error, "Không thể cập nhật trạng thái kỳ thi và ca thi.");
            }

            var now = DateTime.UtcNow;
            var nextMinute = new DateTime(
                now.Ticks - now.Ticks % TimeSpan.TicksPerMinute,
                DateTimeKind.Utc).AddMinutes(1);
            await Task.Delay(nextMinute - now, stoppingToken);
        }
    }
}