namespace LibApp.Core.Policies;

public static class BorrowRequestStatusStyle
{
    /// <summary>Цветовая пара для бейджа статуса заявки. Лейбл — через <c>DisplayName()</c>.</summary>
    public static (string Background, string Foreground) Get(BorrowRequestStatus status) => status switch
    {
        BorrowRequestStatus.Pending => ("#fff3d9", "#8a4b00"),
        BorrowRequestStatus.Approved => ("#e6f7ff", "#005a9e"),
        BorrowRequestStatus.Rejected => ("#fde7e9", "#a30000"),
        BorrowRequestStatus.Cancelled => ("#f3f3f3", "#555"),
        BorrowRequestStatus.Expired => ("#f3f3f3", "#888"),
        BorrowRequestStatus.Completed => ("#e6f7eb", "#0a7f2e"),
        _ => ("#f3f3f3", "#555")
    };
}