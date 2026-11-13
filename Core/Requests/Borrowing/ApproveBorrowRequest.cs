namespace LibApp.Core.Requests.Borrowing;

/// <summary>
/// Одобрение заявки библиотекарем. Срок возврата в запросе не передаётся:
/// Approve — это активация брони (BorrowRequest.ExpiresAt), а не выдача.
/// Срок выдачи определяется при оформлении IssueBorrowRequestCommand.
/// </summary>
public class ApproveBorrowRequest : IAddOrUpdateRequest
{
    public Guid BorrowRequestId { get; set; }
}