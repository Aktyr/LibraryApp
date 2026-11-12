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
        group.MapGet("/search", SearchAsync);
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
    private static Task<IResult> SearchAsync(
        string? query,
        string? title,
        string? author,
        string? genre,
        string? publisher,
        int? yearFrom,
        int? yearTo,
        bool? availableOnly,
        string? sortBy,
        bool? sortDescending,
        int? pageNumber,
        int? pageSize,
        IHttpClientFactory httpFactory,
        CancellationToken ct)
    {
        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(query)) q.Add($"query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrWhiteSpace(title)) q.Add($"title={Uri.EscapeDataString(title)}");
        if (!string.IsNullOrWhiteSpace(author)) q.Add($"author={Uri.EscapeDataString(author)}");
        if (!string.IsNullOrWhiteSpace(genre)) q.Add($"genre={Uri.EscapeDataString(genre)}");
        if (!string.IsNullOrWhiteSpace(publisher)) q.Add($"publisher={Uri.EscapeDataString(publisher)}");
        if (yearFrom.HasValue) q.Add($"yearFrom={yearFrom.Value}");
        if (yearTo.HasValue) q.Add($"yearTo={yearTo.Value}");
        if (availableOnly == true) q.Add("availableOnly=true");
        if (!string.IsNullOrWhiteSpace(sortBy)) q.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
        if (sortDescending == true) q.Add("sortDescending=true");

        // Пагинация
        if (pageNumber.HasValue) q.Add($"pageNumber={pageNumber.Value}");
        if (pageSize.HasValue) q.Add($"pageSize={pageSize.Value}");

        var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
        return ProxyForwarder.ForwardAsync(
            httpFactory, HttpMethod.Get, $"/api/search/books{qs}", null, ct);
    }
}