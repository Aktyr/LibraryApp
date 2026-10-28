namespace LibApp.Core.Requests.Reports;

public class GetDiscardedBooksRequest : IGetRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DiscardReason? DiscardReason { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? ApprovedById { get; set; }
    public Guid? BookId { get; set; }
}