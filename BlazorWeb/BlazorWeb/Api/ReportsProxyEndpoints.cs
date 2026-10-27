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
    }

    private static Task<IResult> GetPopularBooksAsync(
        int? topCount, DateTime? fromDate, DateTime? toDate,
        IHttpClientFactory httpFactory, CancellationToken ct)
    {
        var query = $"?topCount={topCount ?? 10}";
        if (fromDate.HasValue) query += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
        if (toDate.HasValue) query += $"&toDate={toDate.Value:yyyy-MM-dd}";

        return ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get,
            $"/api/reports/popular-books{query}", null, ct);
    }

    private static Task<IResult> GetUserActivityAsync(
        bool onlyWithOverdue, bool onlyActive,
        IHttpClientFactory httpFactory, CancellationToken ct)
    {
        var query = $"?onlyWithOverdue={onlyWithOverdue}&onlyActive={onlyActive}";
        return ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get,
            $"/api/reports/user-activity{query}", null, ct);
    }
}