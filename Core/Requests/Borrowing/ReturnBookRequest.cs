namespace LibApp.Core.Requests.Borrowing;

public class ReturnBookRequest : IAddOrUpdateRequest
{
    public Guid UserRoomBookId { get; set; }
    public DiscardReason? DiscardReason { get; set; }
    public decimal? CompensationAmount { get; set; }
}