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
            _logger.LogInformation("Check interval updated to {Hours} hours", _checkInterval.TotalHours);
        });
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Стартовый delay: хост поднимается параллельно с миграциями. Без задержки
        // первый прогон может удариться в БД до применения миграций. Минута — запас.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CheckDeadlines(cancellationToken);
                await Task.Delay(_checkInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking deadlines");
                try { await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }

    private async Task CheckDeadlines(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var settings = _settingsMonitor.CurrentValue;
        var penaltySettings = _penaltySettingsMonitor.CurrentValue;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Напоминание: только те, кому ещё не отправляли. ReminderSentAt снимает
        // повторную отправку при рестарте приложения и на следующем цикле.
        var soonDue = await userRoomBookRepo.GetAsync(
            urb => urb.ReturnDate == null
                && urb.Deadline.HasValue
                && urb.Deadline.Value.Date == now.AddDays(settings.ReturnReminderInDays).Date
                && urb.ReminderSentAt == null,
            cancellationToken);

        foreach (var urb in soonDue.ToList())
        {
            await notificationService.SendReturnReminderAsync(urb.User, urb, cancellationToken);
            urb.ReminderSentAt = now;
            await userRoomBookRepo.UpdateAsync(urb, cancellationToken);
        }

        // Просрочки: только те, кому ещё не отправляли уведомление. Штраф
        // пересчитываем всегда — он растёт, пока книга не возвращена. Но аудит пишем
        // только если сумма изменилась, иначе журнал спамится одинаковыми записями.
        var overdue = await userRoomBookRepo.GetAsync(
            urb => urb.ReturnDate == null
                && urb.Deadline.HasValue
                && urb.Deadline.Value < now,
            cancellationToken);

        var overdueList = overdue.ToList();
        decimal totalAccrued = 0m;
        var anyPenaltyChanged = false;

        foreach (var urb in overdueList)
        {
            var daysOverdue = (now - urb.Deadline!.Value).Days;
            var penalty = PenaltyCalculatorService.CalculateFromDays(daysOverdue, penaltySettings);

            if (urb.Penalty != penalty)
            {
                urb.Penalty = penalty;
                totalAccrued += penalty;
                anyPenaltyChanged = true;
                await userRoomBookRepo.UpdateAsync(urb, cancellationToken);
            }

            if (urb.OverdueNoticeSentAt == null)
            {
                await notificationService.SendOverdueNotificationAsync(urb.User, urb, penalty, cancellationToken);
                urb.OverdueNoticeSentAt = now;
                await userRoomBookRepo.UpdateAsync(urb, cancellationToken);
            }
        }

        // Аудит только при фактическом изменении суммы штрафов. Пустой цикл
        // (ни одного изменения) не должен порождать запись в журнале.
        if (anyPenaltyChanged)
        {
            await auditService.LogAsync(
                AuditActionType.PenaltyAccrued,
                entityType: nameof(UserRoomBook),
                entityId: null,
                details: $"Пересчитаны штрафы: {overdueList.Count} выдач, сумма {totalAccrued:0.##} ₽",
                userId: null,
                userName: "Система: пересчёт штрафов",
                ct: cancellationToken);
        }

        // SaveChanges вызывается всегда: soonDue мог обновить ReminderSentAt,
        // даже если просрочек нет. Фиксация в одной транзакции с аудитом.
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}