using LibApp.Core.Requests.Reports;

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
        var request = new GetDiscardedBooksRequest
        {
            FromDate = fromDate,
            ToDate = toDate,
            DiscardReason = discardReason,
            RoomId = roomId,
            ApprovedById = approvedById,
            BookId = bookId
        };
        return ProxyForwarder.ForwardAsync(
            httpFactory, HttpMethod.Get,
            $"/api/books/discarded{request.BuildQueryString()}", null, ct);
    }

}