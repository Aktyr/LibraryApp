using BlazorWeb.Client.Services.Interfaces;

namespace BlazorWeb.Client.Services.APIs;

public class ReportsApiClient(HttpClient http) : IReportsApiClient
{
    private const string BasePath = "/api/proxy/reports";

    public async Task<BookPopularityReportResponse> GetPopularBooksAsync(
        int? topCount = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (topCount.HasValue) query.Add($"topCount={topCount.Value}");
        if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await http.GetFromJsonAsync<BookPopularityReportResponse>($"{BasePath}/popular-books{queryString}", ct)
               ?? new BookPopularityReportResponse("Error", "Empty response", []);
    }

    public async Task<UserActivityReportResponse> GetUserActivityAsync(
        bool onlyWithOverdue = false, bool onlyActive = false,
        DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var query = $"?onlyWithOverdue={onlyWithOverdue}&onlyActive={onlyActive}";
        if (fromDate.HasValue) query += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
        if (toDate.HasValue) query += $"&toDate={toDate.Value:yyyy-MM-dd}";

        return await http.GetFromJsonAsync<UserActivityReportResponse>(
                   $"{BasePath}/user-activity{query}", ct)
               ?? new UserActivityReportResponse("Error", "Empty response", []);
    }

    public async Task<CurrentIssueReportResponse> GetCurrentIssuesAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<CurrentIssueReportResponse>($"{BasePath}/current-issues", ct)
            ?? new CurrentIssueReportResponse("Error", "Empty response", []);
}