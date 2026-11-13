namespace LibApp.Core.Requests.Borrowing;

/// <summary>
/// Одобрение заявки библиотекарем. <see cref="BorrowDays"/> — срок выдачи в днях,
/// тот же, что в прямой выдаче; валидируется в <c>BorrowingValidatorAsync</c> по настройкам
/// <c>BorrowingSettings</c>.
/// </summary>
public class ApproveBorrowRequest : IAddOrUpdateRequest
{
    public Guid BorrowRequestId { get; set; }
    public int BorrowDays { get; set; }
}