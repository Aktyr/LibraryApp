namespace LibApp.Core.Requests.Reports;

public class GetUserActivityRequest : IGetRequest
{
    public bool OnlyWithOverdue { get; set; }  // Только должники
    public bool OnlyActive { get; set; }       // Только активные
    public DateTime? FromDate { get; set; }    // За период с
    public DateTime? ToDate { get; set; }      // За период по
}
