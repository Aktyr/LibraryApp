namespace LibApp.Application.Services;

public class PenaltyConfiguration // todo перенести в отдельный файл
{
    public decimal DailyRate { get; set; } = 10;        // 10 руб/день
    public decimal? MaxPenalty { get; set; } = 500;     // Максимальный штраф 500 руб (null - снять ограничение)
    public int GracePeriodDays { get; set; } = 0;       // Не штрафуемый период (дней)
    public void Validate() // todo вынести в отдельный валидатор
    {
        if (DailyRate <= 0)
            throw new InvalidOperationException("DailyRate must be greater than 0");

        if (MaxPenalty.HasValue && MaxPenalty.Value <= 0)
            throw new InvalidOperationException("MaxPenalty must be greater than 0");

        if (GracePeriodDays < 0)
            throw new InvalidOperationException("GracePeriodDays cannot be negative");
    }
}

// todo добавить штрафы при порче/потери книги
public class PenaltyCalculatorService: IService
{
    private readonly PenaltyConfiguration _config;
    private readonly ILogger<PenaltyCalculatorService>? _logger;

    public PenaltyCalculatorService(PenaltyConfiguration config, ILogger<PenaltyCalculatorService>? logger = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();
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
        if (!deadline.HasValue)
        {
            _logger?.LogDebug("Deadline is null, penalty = 0");
            return 0;
        }

        var now = currentDate ?? DateTime.Now;

        // Добавляем льготный период
        var gracePeriodEnd = deadline.Value.AddDays(_config.GracePeriodDays);

        if (now <= gracePeriodEnd)
        {
            _logger?.LogDebug($"Within grace period. Deadline: {deadline.Value}, Now: {now}, Penalty = 0");
            return 0;
        }

        // Рассчитываем дни просрочки с округлением вверх (учитывая часы)
        var totalDaysOverdue = (now - deadline.Value).TotalDays;
        var actualDaysOverdue = (int)Math.Ceiling(totalDaysOverdue) - _config.GracePeriodDays;

        // Защита от отрицательных значений
        if (actualDaysOverdue <= 0)
        {
            _logger?.LogWarning($"Unexpected negative overdue days: {actualDaysOverdue}");
            return 0;
        }

        var penalty = actualDaysOverdue * _config.DailyRate;

        // Округляем до копеек
        penalty = Math.Round(penalty, 2, MidpointRounding.AwayFromZero);

        // Применяем ограничение максимального штрафа
        if (_config.MaxPenalty.HasValue && penalty > _config.MaxPenalty.Value)  // Добавить HasValue и Value
        {
            _logger?.LogInformation($"Penalty {penalty} exceeds max penalty {_config.MaxPenalty.Value}. Applying cap.");  // Добавить Value
            return _config.MaxPenalty.Value;  // Добавить Value
        }


        _logger?.LogDebug($"Penalty calculated: DaysOverdue={actualDaysOverdue}, DailyRate={_config.DailyRate}, Penalty={penalty}");

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

            if (maxPenalty > userRoomBook.Penalty.Value)
                _logger?.LogInformation($"Penalty increased from {userRoomBook.Penalty.Value} to {currentPenaltyValue}");

            return maxPenalty;
        }

        // Первое начисление штрафа
        _logger?.LogDebug($"First penalty calculation: {currentPenalty}");
        return currentPenalty;
    }
}

