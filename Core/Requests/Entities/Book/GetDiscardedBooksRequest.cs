namespace LibApp.Core.Requests.Entities.Book;

public class GetDiscardedBooksRequest : IGetRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DiscardReason DiscardReason { get; set; }
}