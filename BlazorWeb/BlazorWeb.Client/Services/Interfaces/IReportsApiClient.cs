namespace BlazorWeb.Client.Services.Interfaces;

public interface IReportsApiClient
{
    Task<BookPopularityReportResponse> GetPopularBooksAsync(int? topCount = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
    Task<UserActivityReportResponse> GetUserActivityAsync(bool onlyWithOverdue = false, bool onlyActive = false, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);

    /// <summary>Книги на руках сейчас, включая выдачи деактивированных читателей. Фильтрация на клиенте.</summary>
    Task<CurrentIssueReportResponse> GetCurrentIssuesAsync(CancellationToken ct = default);
}