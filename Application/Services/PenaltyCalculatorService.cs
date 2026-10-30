namespace LibApp.Application.Services;

public class PenaltyCalculatorService : IService
{
    // todo добавить штрафы при порче/потери книги
    private readonly IOptionsSnapshot<PenaltySettings> _settings;
    private readonly ILogger<PenaltyCalculatorService>? _logger;

    public PenaltyCalculatorService(IOptionsSnapshot<PenaltySettings> settings, ILogger<PenaltyCalculatorService>? logger = null)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Базовая формула штрафа: ставка × дни, округление до копеек, применение cap.
    /// Льготный период сюда не входит — вызывающая сторона уже вычла его из <paramref name="daysOverdue"/>.
    /// </summary>
    public static decimal CalculateFromDays(int daysOverdue, PenaltySettings settings)
    {
        if (daysOverdue <= 0) return 0m;

        var penalty = daysOverdue * settings.DailyRate;
        penalty = Math.Round(penalty, 2, MidpointRounding.AwayFromZero);

        return settings.MaxPenalty.HasValue && penalty > settings.MaxPenalty.Value
            ? settings.MaxPenalty.Value
            : penalty;
    }

    public decimal? CalculatePenalty(DateTime? deadline, DateTime? currentDate = null)
    {
        var config = _settings.Value;

        if (!deadline.HasValue)
        {
            _logger?.LogDebug("Deadline is null, penalty = 0");
            return 0;
        }

        var now = currentDate ?? DateTime.UtcNow;

        var gracePeriodEnd = deadline.Value.AddDays(config.GracePeriodDays);
        if (now <= gracePeriodEnd)
        {
            _logger?.LogDebug("Within grace period. Penalty = 0");
            return 0;
        }

        var totalDaysOverdue = (now - deadline.Value).TotalDays;
        var actualDaysOverdue = (int)Math.Ceiling(totalDaysOverdue) - config.GracePeriodDays;

        if (actualDaysOverdue <= 0)
        {
            _logger?.LogWarning("Unexpected negative overdue days: {Days}", actualDaysOverdue);
            return 0;
        }

        return CalculateFromDays(actualDaysOverdue, config);
    }

    /// <summary>
    /// Рассчитывает итоговый штраф с учетом состояния бронирования
    /// </summary>
    /// <param name="userRoomBook">Объект бронирования</param>
    /// <param name="currentDate">Текущая дата (для тестирования)</param>
    /// <returns>Сумма штрафа</returns>
    /// <exception cref="ArgumentNullException">Если userRoomBook = null</exception>
    public decimal? CalculatePenaltyForReturn(UserRoomBook userRoomBook, DateTime? currentDate = null)
    {
        if (userRoomBook == null)
            throw new ArgumentNullException(nameof(userRoomBook));

        // Если книга возвращена, возвращаем сохраненный штраф
        if (userRoomBook.IsReturned)
        {
            var savedPenalty = userRoomBook.Penalty ?? 0;
            _logger?.LogDebug($"Book is returned. Saved penalty: {savedPenalty}");
            return savedPenalty;
        }

        // Рассчитываем текущий штраф
        var currentPenalty = CalculatePenalty(userRoomBook.Deadline, currentDate);

        // Если штраф уже был начислен ранее
        if (userRoomBook.Penalty.HasValue)
        {
            var currentPenaltyValue = currentPenalty ?? 0;
            var maxPenalty = Math.Max(currentPenaltyValue, userRoomBook.Penalty.Value);
            return maxPenalty;
        }
        // Первое начисление штрафа
        return currentPenalty;
    }
}