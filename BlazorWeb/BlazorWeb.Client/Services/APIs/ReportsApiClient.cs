using BlazorWeb.Client.Services.Interfaces;

namespace BlazorWeb.Client.Services.APIs;

public class ReportsApiClient(HttpClient http) : IReportsApiClient
{
    private const string BasePath = "/api/proxy/reports";

    public async Task<BookPopularityReportResponse> GetPopularBooksAsync(
        int? topCount = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var query = $"?topCount={topCount ?? 10}";
        if (fromDate.HasValue) query += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
        if (toDate.HasValue) query += $"&toDate={toDate.Value:yyyy-MM-dd}";

        return await http.GetFromJsonAsync<BookPopularityReportResponse>($"{BasePath}/popular-books{query}", ct)
               ?? new BookPopularityReportResponse("Error", "Empty response", []);
    }

    public async Task<UserActivityReportResponse> GetUserActivityAsync(
        bool onlyWithOverdue = false, bool onlyActive = false, CancellationToken ct = default)
        => await http.GetFromJsonAsync<UserActivityReportResponse>(
            $"{BasePath}/user-activity?onlyWithOverdue={onlyWithOverdue}&onlyActive={onlyActive}", ct)
           ?? new UserActivityReportResponse("Error", "Empty response", []);
}