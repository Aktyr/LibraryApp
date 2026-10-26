namespace BlazorWeb.Client.Services.Interfaces;

public interface IReportsApiClient
{
    Task<BookPopularityReportResponse> GetPopularBooksAsync(int? topCount = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    Task<UserActivityReportResponse> GetUserActivityAsync(bool onlyWithOverdue = false, bool onlyActive = false, CancellationToken ct = default);
}