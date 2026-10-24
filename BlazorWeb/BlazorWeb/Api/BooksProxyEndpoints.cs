namespace BlazorWeb.Api;

public static class BooksProxyEndpoints
{
    private const string WebApiClientName = "webapi";

    public static void MapBooksProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/books")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static Task<IResult> GetAllAsync(
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, "/api/books", null, ct);

    private static Task<IResult> GetByIdAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, $"/api/books/{id}", null, ct);

    private static Task<IResult> CreateAsync(
        CreateBookRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Post, "/api/books", request, ct);

    private static Task<IResult> UpdateAsync(
        UpdateBookRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Put, "/api/books", request, ct);

    private static Task<IResult> DeleteAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Delete, $"/api/books/{id}", null, ct);

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