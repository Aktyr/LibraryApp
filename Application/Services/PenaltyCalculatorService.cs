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
    /// Рассчитывает штраф на основе срока возврата
    /// </summary>
    /// <param name="deadline">Срок возврата</param>
    /// <param name="currentDate">Текущая дата (для тестирования)</param>
    /// <returns>Сумма штрафа</returns>
    public decimal? CalculatePenalty(DateTime? deadline, DateTime? currentDate = null)
    {
        var config = _settings.Value;

        if (!deadline.HasValue)
        {
            _logger?.LogDebug("Deadline is null, penalty = 0");
            return 0;
        }

        var now = currentDate ?? DateTime.Now;

        // Добавляем льготный период
        var gracePeriodEnd = deadline.Value.AddDays(config.GracePeriodDays);
        if (now <= gracePeriodEnd)
        {
            _logger?.LogDebug($"Within grace period. Penalty = 0");
            return 0;
        }

        // Рассчитываем дни просрочки с округлением вверх (учитывая часы)
        var totalDaysOverdue = (now - deadline.Value).TotalDays;
        var actualDaysOverdue = (int)Math.Ceiling(totalDaysOverdue) - config.GracePeriodDays;

        // Защита от отрицательных значений
        if (actualDaysOverdue <= 0)
        {
            _logger?.LogWarning($"Unexpected negative overdue days: {actualDaysOverdue}");
            return 0;
        }

        var penalty = actualDaysOverdue * config.DailyRate;
        // Округляем до копеек
        penalty = Math.Round(penalty, 2, MidpointRounding.AwayFromZero);

        // Применяем ограничение максимального штрафа
        if (config.MaxPenalty.HasValue && penalty > config.MaxPenalty.Value)
        {
            _logger?.LogInformation($"Penalty {penalty} exceeds max penalty {config.MaxPenalty.Value}. Applying cap.");
            return config.MaxPenalty.Value;
        }

        _logger?.LogDebug($"Penalty calculated: DaysOverdue={actualDaysOverdue}, Penalty={penalty}");
        return penalty;
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