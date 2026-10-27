namespace BlazorWeb.Api;

public static class BooksProxyEndpoints
{
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
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, "/api/books", null, ct);

    private static Task<IResult> GetByIdAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, $"/api/books/{id}", null, ct);

    private static Task<IResult> CreateAsync(
        CreateBookRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post, "/api/books", request, ct);

    private static Task<IResult> UpdateAsync(
        UpdateBookRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Put, "/api/books", request, ct);

    private static Task<IResult> DeleteAsync(
        Guid id, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Delete, $"/api/books/{id}", null, ct);
}