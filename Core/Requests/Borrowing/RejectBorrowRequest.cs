namespace LibApp.Core.Requests.Borrowing;

/// <summary>
/// Отклонение заявки библиотекарем. <see cref="Comment"/> обязателен:
/// отказ без причины бесполезен читателю и для аудита.
/// </summary>
public class RejectBorrowRequest : IAddOrUpdateRequest
{
    public Guid BorrowRequestId { get; set; }
    public string Comment { get; set; } = string.Empty;
}