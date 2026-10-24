namespace BlazorWeb.Api;

public static class BooksProxyEndpoints
{
    private const string WebApiClientName = "webapi";

    public static void MapBooksProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/books")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapGet("/", GetAllAsync).WithName("ProxyBooksGetAll");
        group.MapPost("/", CreateAsync).WithName("ProxyBooksCreate");
    }

    private static async Task<IResult> GetAllAsync(
        IHttpClientFactory httpFactory,
        CancellationToken ct)
    {
        var http = httpFactory.CreateClient(WebApiClientName);
        var resp = await http.GetAsync("/api/books", ct);
        return await ForwardAsync(resp, ct);
    }

    private static async Task<IResult> CreateAsync(
        CreateBookRequest request,
        IHttpClientFactory httpFactory,
        CancellationToken ct)
    {
        var http = httpFactory.CreateClient(WebApiClientName);
        var resp = await http.PostAsJsonAsync("/api/books", request, ct);
        return await ForwardAsync(resp, ct);
    }

    private static async Task<IResult> ForwardAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        var body = await resp.Content.ReadAsStringAsync(ct);
        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/json";
        return Results.Content(body, contentType, statusCode: (int)resp.StatusCode);
    }
}