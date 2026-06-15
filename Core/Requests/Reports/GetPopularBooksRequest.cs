namespace LibApp.Core.Requests.Reports;

public class GetPopularBooksRequest : IGetRequest
{
    public int? TopCount { get; set; } = 10;  // Топ N книг
    public DateTime? FromDate { get; set; }   // За период с
    public DateTime? ToDate { get; set; }     // За период по
}