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
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var settings = _settingsMonitor.CurrentValue;
        var penaltySettings = _penaltySettingsMonitor.CurrentValue;
        var now = DateTime.UtcNow;

        // Книги, которые нужно вернуть через N дня
        var soonDue = await userRoomBookRepo.GetAsync(
            urb => urb.ReturnDate == null
                && urb.Deadline.HasValue
                && urb.Deadline.Value.Date == now.AddDays(settings.ReturnReminderInDays).Date,
            cancellationToken);

        foreach (var urb in soonDue.ToList())
            await notificationService.SendReturnReminderAsync(urb.User, urb, cancellationToken);

        // Просроченные книги
        var overdue = await userRoomBookRepo.GetAsync(
            urb => urb.ReturnDate == null
                && urb.Deadline.HasValue
                && urb.Deadline.Value < now,
            cancellationToken);

        var overdueList = overdue.ToList();

        // Нет просрочек — нечего сохранять. Аудит не пишем: иначе журнал спамится
        // пустыми записями каждый цикл.
        if (overdueList.Count == 0)
            return;

        decimal totalAccrued = 0m;

        foreach (var urb in overdueList)
        {
            var daysOverdue = (now - urb.Deadline!.Value).Days;
            var penalty = PenaltyCalculatorService.CalculateFromDays(daysOverdue, penaltySettings);

            // Обновляем штраф в БД
            urb.Penalty = penalty;
            totalAccrued += penalty;
            await userRoomBookRepo.UpdateAsync(urb, cancellationToken);

            // Отправляем уведомление
            await notificationService.SendOverdueNotificationAsync(urb.User, urb, penalty, cancellationToken);
        }

        // Сводный аудит batch-а: одна запись вместо N. Раз в сутки по числу просрочек
        // раздуло бы журнал. Детализация по каждой выдаче — в UserRoomBook.Penalty.
        // userId = null, userName явно — фоновый процесс, ICurrentUserService пуст.
        await auditService.LogAsync(
            AuditActionType.PenaltyAccrued,
            entityType: nameof(UserRoomBook),
            entityId: null,
            details: $"Пересчитаны штрафы: {overdueList.Count} выдач, сумма {totalAccrued:0.##} ₽",
            userId: null,
            userName: "Система: пересчёт штрафов",
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <remarks>
    /// Оставлен для юнит-тестов (проверяют формулу через рефлексию).
    /// В рабочем коде используем <see cref="PenaltyCalculatorService.CalculateFromDays"/>.
    /// </remarks>
    private static decimal CalculatePenalty(int daysOverdue, PenaltySettings settings)
        => PenaltyCalculatorService.CalculateFromDays(daysOverdue, settings);
}