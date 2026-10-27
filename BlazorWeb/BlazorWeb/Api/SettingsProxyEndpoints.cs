namespace BlazorWeb.Api;

public static class SettingsProxyEndpoints
{
    private const string WebApiClientName = "webapi";

    public static void MapSettingsProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/settings")
            .WithTags("Proxy");

        group.MapGet("/borrowing", GetBorrowingAsync);
    }

    private static Task<IResult> GetBorrowingAsync(IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, "/api/settings/borrowing", null, ct);

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