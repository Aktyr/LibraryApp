namespace LibApp.Core.Requests.Borrowing;

/// <summary>
/// Отмена заявки читателем. Доступно для Pending и Approved; для остальных статусов
/// команда отклонит запрос. Причина необязательна: если читатель передумал — достаточно факта.
/// </summary>
public class CancelBorrowRequest : IAddOrUpdateRequest
{
    public Guid BorrowRequestId { get; set; }
    public string? Comment { get; set; }
}