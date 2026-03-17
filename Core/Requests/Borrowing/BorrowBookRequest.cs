namespace LibApp.Core.Requests.Borrowing;

public class BorrowBookRequest : IAddOrUpdateRequest
{
    public Guid UserId { get; set; }
    public Guid RoomBookId { get; set; }
    public int BorrowDays { get; set; } // Количество дней до сдачи
    public DateTime Deadline => DateTime.Now.AddDays(BorrowDays);
}

