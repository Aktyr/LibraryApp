namespace LibApp.Core.Requests.Borrowing;

/// <summary>Оформление фактической выдачи по одобренной брони.</summary>
public class IssueBorrowRequest : IAddOrUpdateRequest
{
    public Guid BorrowRequestId { get; set; }
    public int BorrowDays { get; set; }
}