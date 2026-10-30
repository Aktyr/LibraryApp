namespace LibApp.Core.Requests.Reports;

public class GetDiscardedBooksRequest : IGetRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DiscardReason? DiscardReason { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? ApprovedById { get; set; }
    public Guid? BookId { get; set; }

    /// <summary>
    /// Собирает query-string с ведущим «?» или пустую строку, если фильтров нет.
    /// Формат даты стандартный для DB — <c>yyyy-MM-dd</c>.
    /// </summary>
    public string BuildQueryString()
    {
        var q = new List<string>();
        if (FromDate.HasValue) q.Add($"fromDate={FromDate.Value:yyyy-MM-dd}");
        if (ToDate.HasValue) q.Add($"toDate={ToDate.Value:yyyy-MM-dd}");
        if (DiscardReason.HasValue) q.Add($"discardReason={DiscardReason.Value}");
        if (RoomId.HasValue) q.Add($"roomId={RoomId.Value}");
        if (ApprovedById.HasValue) q.Add($"approvedById={ApprovedById.Value}");
        if (BookId.HasValue) q.Add($"bookId={BookId.Value}");
        return q.Count > 0 ? "?" + string.Join("&", q) : "";
    }
}