namespace LibApp.Core.Requests.Reports;

public class GetPopularBooksRequest : IGetRequest
{
    public int? TopCount { get; set; }        // Топ N книг; null — без усечения
    public DateTime? FromDate { get; set; }   // За период с
    public DateTime? ToDate { get; set; }     // За период по
}