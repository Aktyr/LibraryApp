namespace LibApp.Application.Services;

public class DeadlineCheckService : BackgroundService
{
    // Параметры
    private const int RETURN_REMINDER_IN_DAYS = 3;
    private const int CHECK_INTERVAL_IN_HOURS = 24;
    private const decimal DAILY_PENALTY = 10;
    private const decimal MAX_PENALTY = 500;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeadlineCheckService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(CHECK_INTERVAL_IN_HOURS);

    public DeadlineCheckService(IServiceProvider serviceProvider, ILogger<DeadlineCheckService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
        var userRoomBookRepo = scope.ServiceProvider.GetRequiredService<IRepository<UserRoomBook>>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.Now;

        // Книги, которые нужно вернуть через N дня
        var soonDue = await userRoomBookRepo.Get(
            urb => !urb.IsReturned
                && urb.Deadline.HasValue
                && urb.Deadline.Value.Date == now.AddDays(RETURN_REMINDER_IN_DAYS).Date,
            cancellationToken);

        foreach (var urb in soonDue)
        {
            await notificationService.SendReturnReminderAsync(urb.User, urb, cancellationToken);
        }

        // Просроченные книги
        var overdue = await userRoomBookRepo.Get(
            urb => !urb.IsReturned
                && urb.Deadline.HasValue
                && urb.Deadline.Value < now,
            cancellationToken);

        foreach (var urb in overdue)
        {
            var daysOverdue = (now - urb.Deadline!.Value).Days;
            var penalty = CalculatePenalty(daysOverdue);
            await notificationService.SendOverdueNotificationAsync(urb.User, urb, penalty, cancellationToken);
        }
    }
    private decimal CalculatePenalty(int daysOverdue)
    {
        var penalty = daysOverdue * DAILY_PENALTY;
        return Math.Min(penalty, MAX_PENALTY);
    }
}
