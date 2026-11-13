namespace LibApp.Application.Services;

/// <summary>
/// Фоновый сервис истечения броней. Переводит BorrowRequest.Status = Approved
/// в Expired, если ExpiresAt < now, и декрементирует RoomBook.ReservedCount:
/// экземпляр возвращается в оборот. Работает по аналогии с DeadlineCheckService,
/// но с двумя отличиями: время через TimeProvider (детерминизм в тестах)
/// и батч-аудит с проверкой «были ли истёкшие» — пустые циклы не пишут в журнал.
/// </summary>
public class ExpireBorrowRequestsService : BackgroundService, IService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpireBorrowRequestsService> _logger;
    private readonly IOptionsMonitor<BorrowingSettings> _settingsMonitor;
    private TimeSpan _checkInterval;

    public ExpireBorrowRequestsService(
        IServiceProvider serviceProvider,
        ILogger<ExpireBorrowRequestsService> logger,
        IOptionsMonitor<BorrowingSettings> settingsMonitor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settingsMonitor = settingsMonitor;
        _checkInterval = TimeSpan.FromMinutes(_settingsMonitor.CurrentValue.ReserveExpiryCheckIntervalMinutes);

        _settingsMonitor.OnChange(newSettings =>
        {
            _checkInterval = TimeSpan.FromMinutes(newSettings.ReserveExpiryCheckIntervalMinutes);
            _logger.LogInformation(
                "Borrow request expiry check interval updated to {Minutes} minutes",
                _checkInterval.TotalMinutes);
        });
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Стартовый delay: хост поднимается параллельно с миграциями и прогревом,
        // первый запрос к БД сразу после старта может упасть. Минута — запас.
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
                await ExpireAsync(cancellationToken);
                await Task.Delay(_checkInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring borrow requests");
                try { await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }

    /// <summary>
    /// Один цикл проверки истечения броней. <c>protected</c> — для юнит-тестов,
    /// которые вызывают его напрямую, минуя <see cref="ExecuteAsync"/> с его
    /// <c>Task.Delay</c>. Возвращает количество переведённых в Expired заявок.
    /// </summary>
    protected async Task<int> ExpireAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var repo = unitOfWork.GetRepository<BorrowRequest>();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var expiredCount = 0;

        // Транзакция на весь цикл: ReservedCount и статус меняются вместе с аудитом.
        // При падении на любом шаге откатывается всё — консистентность важнее скорости.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var expiredRequests = await repo.GetAsync(
                filter: br => br.Status == BorrowRequestStatus.Approved
                    && br.ExpiresAt != null
                    && br.ExpiresAt < now,
                cancellationToken: cancellationToken,
                includePaths: [IncludePaths.BorrowRequest.RoomBook]);

            // Защитный фильтр: в EF-запросе строки с ExpiresAt = null отсекаются,
            // но если фильтр когда-нибудь ослабят, заявка без срока не должна попасть
            // в Expired — она не была бронирована, и уменьшать ReservedCount не за что.
            // Фильтрация до счётчика гарантирует, что expiredCount совпадает с числом
            // реально обработанных заявок, а не с размером сырого списка из репозитория.
            var expiredList = expiredRequests
                .Where(br => br.ExpiresAt.HasValue)
                .ToList();

            // Пустой цикл — нечего сохранять, нечего логировать. Без этой проверки
            // журнал получил бы по записи каждые 15 минут даже в отсутствие истёкших.
            if (expiredList.Count == 0)
                return;

            foreach (var borrowRequest in expiredList)
            {
                borrowRequest.Status = BorrowRequestStatus.Expired;
                borrowRequest.RoomBook.ReservedCount--;
            }

            // Одна запись за цикл, а не по одной на заявку. EntityId = null:
            // у сводки нет единственной сущности-цели. Конкретные заявки видны
            // в истории читателя и в /requests по своим статусам.
            await auditService.LogAsync(
                AuditActionType.Update,
                nameof(BorrowRequest),
                entityId: null,
                details: $"Истекло броней: {expiredList.Count}",
                userId: null,
                userName: "Система: истечение броней",
                ct: cancellationToken);

            _logger.LogInformation("Expired {Count} borrow requests", expiredList.Count);
            expiredCount = expiredList.Count;
        }, cancellationToken);

        return expiredCount;
    }
}