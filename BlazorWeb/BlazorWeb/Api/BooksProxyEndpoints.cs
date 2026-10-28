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
        group.MapPost("/discard", DiscardAsync);
        group.MapGet("/discarded", GetDiscardedAsync);

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

    private static Task<IResult> DiscardAsync(
        DiscardBookRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post, "/api/books/discard", request, ct);
    private static Task<IResult> GetDiscardedAsync(
        DateTime? fromDate,
        DateTime? toDate,
        DiscardReason? discardReason,
        Guid? roomId,
        Guid? approvedById,
        Guid? bookId,
        IHttpClientFactory httpFactory,
        CancellationToken ct)
    {
        var q = new List<string>();
        if (fromDate.HasValue) q.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) q.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        if (discardReason.HasValue) q.Add($"discardReason={discardReason.Value}");
        if (roomId.HasValue) q.Add($"roomId={roomId.Value}");
        if (approvedById.HasValue) q.Add($"approvedById={approvedById.Value}");
        if (bookId.HasValue) q.Add($"bookId={bookId.Value}");

        var query = q.Count > 0 ? "?" + string.Join("&", q) : "";
        return ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, $"/api/books/discarded{query}", null, ct);
    }

}