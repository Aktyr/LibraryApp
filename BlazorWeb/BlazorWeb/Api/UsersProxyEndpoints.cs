namespace BlazorWeb.Api;

public static class UsersProxyEndpoints
{
    private const string WebApiClientName = "webapi";

    public static void MapUsersProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/users")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/{id:guid}/books", GetUserBooksAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static Task<IResult> GetAllAsync(
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, "/api/users", null, ct);

    private static Task<IResult> GetByIdAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, $"/api/users/{id}", null, ct);

    private static Task<IResult> GetUserBooksAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, $"/api/users/{id}/books", null, ct);

    private static Task<IResult> CreateAsync(
        CreateUserRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Post, "/api/users", request, ct);

    private static Task<IResult> UpdateAsync(
        UpdateUserRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Put, "/api/users", request, ct);

    private static Task<IResult> DeleteAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Delete, $"/api/users/{id}", null, ct);

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