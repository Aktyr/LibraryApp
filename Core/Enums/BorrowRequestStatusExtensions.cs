namespace LibApp.Core.Enums;

public static class BorrowRequestStatusExtensions
{
    public static string DisplayName(this BorrowRequestStatus status) => status switch
    {
        BorrowRequestStatus.Pending => "Ожидает",
        BorrowRequestStatus.Approved => "Забронирована",
        BorrowRequestStatus.Rejected => "Отклонена",
        BorrowRequestStatus.Cancelled => "Отменена",
        BorrowRequestStatus.Expired => "Истекла",
        BorrowRequestStatus.Completed => "Выдана",
        _ => status.ToString()
    };
}