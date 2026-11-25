namespace BlazorWeb.Api;

public static class ReportsProxyEndpoints
{
    public static void MapReportsProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/reports")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapGet("/popular-books", GetPopularBooksAsync);
        group.MapGet("/user-activity", GetUserActivityAsync);
        group.MapGet("/current-issues", GetCurrentIssuesAsync);
    }

    private static Task<IResult> GetPopularBooksAsync(
        int? topCount, DateTime? fromDate, DateTime? toDate,
        IHttpClientFactory httpFactory, CancellationToken ct)
    {
        var query = new List<string>();
        if (topCount.HasValue) query.Add($"topCount={topCount.Value}");
        if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get,
            $"/api/reports/popular-books{queryString}", null, ct);
    }

    private static Task<IResult> GetUserActivityAsync(
        bool onlyWithOverdue, bool onlyActive, DateTime? fromDate, DateTime? toDate,
        IHttpClientFactory httpFactory, CancellationToken ct)
    {
        var query = $"?onlyWithOverdue={onlyWithOverdue}&onlyActive={onlyActive}";
        if (fromDate.HasValue) query += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
        if (toDate.HasValue) query += $"&toDate={toDate.Value:yyyy-MM-dd}";

        return ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get,
            $"/api/reports/user-activity{query}", null, ct);
    }

    private static Task<IResult> GetCurrentIssuesAsync(
        IHttpClientFactory httpFactory, CancellationToken ct)
            => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get,
                "/api/reports/current-issues", null, ct);
}