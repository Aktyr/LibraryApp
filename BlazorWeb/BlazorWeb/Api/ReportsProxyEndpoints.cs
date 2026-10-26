namespace BlazorWeb.Api;

public static class ReportsProxyEndpoints
{
    private const string WebApiClientName = "webapi";

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

        return ForwardAsync(httpFactory, HttpMethod.Get, $"/api/reports/popular-books{query}", null, ct);
    }

    private static Task<IResult> GetUserActivityAsync(
        bool onlyWithOverdue, bool onlyActive,
        IHttpClientFactory httpFactory, CancellationToken ct)
    {
        var query = $"?onlyWithOverdue={onlyWithOverdue}&onlyActive={onlyActive}";
        return ForwardAsync(httpFactory, HttpMethod.Get, $"/api/reports/user-activity{query}", null, ct);
    }

    private static async Task<IResult> ForwardAsync(
        IHttpClientFactory httpFactory, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var http = httpFactory.CreateClient(WebApiClientName);
        using var req = new HttpRequestMessage(method, path);
        if (body is not null) req.Content = JsonContent.Create(body);

        using var resp = await http.SendAsync(req, ct);
        var content = await resp.Content.ReadAsStringAsync(ct);
        var mediaType = resp.Content.Headers.ContentType?.MediaType ?? "application/json";
        return Results.Content(content, mediaType, statusCode: (int)resp.StatusCode);
    }
}