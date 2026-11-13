namespace LibApp.Application.Configuration;

[MinMaxValidator(nameof(MinBorrowDays), nameof(MaxBorrowDays))]
public class BorrowingSettings : ISettings
{
    [PositiveNumber]
    [MaxRange(10)]
    public int MaxBooksPerUser { get; set; } = 5;

    [PositiveNumber]
    [MaxRange(14)]
    public int MaxExtendDeadlineDays { get; set; } = 14;

    [PositiveNumber]
    public int MinBorrowDays { get; set; } = 1;

    [PositiveNumber]
    public int MaxBorrowDays { get; set; } = 30;

    /// <summary>
    /// Срок действия брони после Approve. Читатель должен забрать книгу до истечения,
    /// иначе фоновый сервис переводит заявку в Expired, а ReservedCount возвращается в оборот.
    /// </summary>
    [PositiveNumber]
    [MaxRange(168)] // 7 дней
    public int ReserveHoldHours { get; set; } = 48;

    /// <summary>
    /// Максимум активных (Pending + Approved) заявок у одного читателя.
    /// Защита от бронирования всей библиотеки одним пользователем.
    /// </summary>
    [PositiveNumber]
    [MaxRange(50)]
    public int MaxActiveRequestsPerUser { get; set; } = 5;

    /// <summary>
    /// Периодичность фоновой проверки истечения броней в минутах.
    /// 15 минут — компромисс: бронь не висит «мёртвой» после ExpiresAt дольше,
    /// но БД не опрашивается вхолостую каждую минуту.
    /// </summary>
    [PositiveNumber]
    [MaxRange(1440)]
    public int ReserveExpiryCheckIntervalMinutes { get; set; } = 15;
}