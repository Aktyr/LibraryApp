namespace LibApp.Application.Services;

public class DeadlineCheckService : BackgroundService, IService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeadlineCheckService> _logger;
    private readonly IOptionsMonitor<DeadlineCheckSettings> _settingsMonitor;
    private readonly IOptionsMonitor<PenaltySettings> _penaltySettingsMonitor;
    private TimeSpan _checkInterval;

    public DeadlineCheckService(IServiceProvider serviceProvider,
                                ILogger<DeadlineCheckService> logger,
                                IOptionsMonitor<DeadlineCheckSettings> settingsMonitor,
                                IOptionsMonitor<PenaltySettings> penaltySettingsMonitor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settingsMonitor = settingsMonitor;
        _penaltySettingsMonitor = penaltySettingsMonitor;
        _checkInterval = TimeSpan.FromHours(_settingsMonitor.CurrentValue.CheckIntervalInHours);

        // Подписываемся на изменения настроек
        _settingsMonitor.OnChange(newSettings =>
        {
            _checkInterval = TimeSpan.FromHours(newSettings.CheckIntervalInHours);
            _logger.LogInformation($"Check interval updated to {_checkInterval.TotalHours} hours");
        });
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CheckDeadlines(cancellationToken);
                await Task.Delay(_checkInterval, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking deadlines");
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }
    }

    private async Task CheckDeadlines(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope(); //todo ??
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>(); 
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();


        var settings = _settingsMonitor.CurrentValue;
        var penaltySettings = _penaltySettingsMonitor.CurrentValue;
        var now = DateTime.UtcNow;

        // Книги, которые нужно вернуть через N дня
        var soonDue = await userRoomBookRepo.Get(
            urb => !urb.IsReturned
                && urb.Deadline.HasValue
                && urb.Deadline.Value.Date == now.AddDays(settings.ReturnReminderInDays).Date,
            cancellationToken);

        foreach (var urb in soonDue)
            await notificationService.SendReturnReminderAsync(urb.User, urb, cancellationToken);


        // Просроченные книги
        var overdue = await userRoomBookRepo.Get(
            urb => !urb.IsReturned
                && urb.Deadline.HasValue
                && urb.Deadline.Value < now,
            cancellationToken);

        foreach (var urb in overdue)
        {
            var daysOverdue = (now - urb.Deadline!.Value).Days;
            var penalty = CalculatePenalty(daysOverdue, penaltySettings);

            // Обновляем штраф в БД
            urb.Penalty = penalty;
            await userRoomBookRepo.Update(urb, cancellationToken);
            // Отправляем уведомление
            await notificationService.SendOverdueNotificationAsync(urb.User, urb, penalty, cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private decimal CalculatePenalty(int daysOverdue, PenaltySettings settings)
    {
        var penalty = daysOverdue * settings.DailyRate;
        return settings.MaxPenalty.HasValue
            ? Math.Min(penalty, settings.MaxPenalty.Value)
            : penalty;
    }
}