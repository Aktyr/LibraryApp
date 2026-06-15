namespace LibApp.Core.Requests.Reports;

public class GetUserActivityRequest : IGetRequest
{
    public bool OnlyWithOverdue { get; set; }  // Только должники
    public bool OnlyActive { get; set; }       // Только активные
}
