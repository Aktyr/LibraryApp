namespace BlazorWeb.Api;

public static class BorrowingProxyEndpoints
{
    private const string WebApiClientName = "webapi";

    public static void MapBorrowingProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/borrowing")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapPost("/borrow", BorrowAsync);
        group.MapPost("/return", ReturnAsync);
        group.MapPost("/extend", ExtendAsync);
    }

    private static Task<IResult> BorrowAsync(
        BorrowBookRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Post, "/api/borrowing/borrow", request, ct);

    private static Task<IResult> ReturnAsync(
        ReturnBookRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Post, "/api/borrowing/return", request, ct);

    private static Task<IResult> ExtendAsync(
        ExtendDeadlineRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Post, "/api/borrowing/extend", request, ct);

    private static async Task<IResult> ForwardAsync(
        IHttpClientFactory httpFactory,
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct)
    {
        var http = httpFactory.CreateClient(WebApiClientName);
        using var req = new HttpRequestMessage(method, path);
        if (body is not null)
            req.Content = JsonContent.Create(body);

        using var resp = await http.SendAsync(req, ct);
        var content = await resp.Content.ReadAsStringAsync(ct);
        var mediaType = resp.Content.Headers.ContentType?.MediaType ?? "application/json";
        return Results.Content(content, mediaType, statusCode: (int)resp.StatusCode);
    }
}