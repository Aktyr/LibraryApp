namespace BlazorWeb.Api;

public static class RoomsProxyEndpoints
{
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
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, "/api/rooms", null, ct);

    private static Task<IResult> GetByIdAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, $"/api/rooms/{id}", null, ct);

    private static Task<IResult> CreateAsync(
        CreateRoomRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post, "/api/rooms", request, ct);

    private static Task<IResult> UpdateAsync(
        UpdateRoomRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Put, "/api/rooms", request, ct);

    private static Task<IResult> DeleteAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Delete, $"/api/rooms/{id}", null, ct);
}