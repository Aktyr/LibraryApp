namespace LibApp.Core.Policies;

/// <summary>
/// Ограничения полей BorrowRequest. Синхронизированы с EF-конфигурацией
/// (BorrowRequestConfiguration) через те же константы.
/// </summary>
public static class BorrowRequestPolicy
{
    /// <summary>
    /// Максимальная длина комментария сотрудника при обработке заявки.
    /// Для Rejected комментарий обязателен (проверяется в RejectBorrowRequestCommand).
    /// </summary>
    public const int MaxCommentLength = 1000;
}