
namespace BlazorWeb.Api;

public static class RoomsProxyEndpoints
{
    private const string WebApiClientName = "webapi";

    public static void MapRoomsProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/rooms")
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
        => ForwardAsync(httpFactory, HttpMethod.Get, "/api/rooms", null, ct);

    private static Task<IResult> GetByIdAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Get, $"/api/rooms/{id}", null, ct);

    private static Task<IResult> CreateAsync(
        CreateRoomRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Post, "/api/rooms", request, ct);

    private static Task<IResult> UpdateAsync(
        UpdateRoomRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Put, "/api/rooms", request, ct);

    private static Task<IResult> DeleteAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ForwardAsync(httpFactory, HttpMethod.Delete, $"/api/rooms/{id}", null, ct);

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