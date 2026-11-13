namespace BlazorWeb.Api;

public static class BorrowRequestProxyEndpoints
{
    public static void MapBorrowRequestProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/borrow-requests")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapPost("/", CreateAsync);
        group.MapGet("/my", GetMyAsync);
        group.MapGet("/", GetAllAsync);
        group.MapPost("/{id:guid}/approve", ApproveAsync);
        group.MapPost("/{id:guid}/reject", RejectAsync);
        group.MapPost("/{id:guid}/issue", IssueAsync);
        group.MapPost("/{id:guid}/cancel", CancelAsync);
    }

    private static Task<IResult> CreateAsync(
        CreateBorrowRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post,
            "/api/borrow-requests", request, ct);

    // userId из query игнорируется: WebAPI берёт его из JWT.
    private static Task<IResult> GetMyAsync(
        BorrowRequestStatus? status,
        Guid? bookId,
        Guid? roomId,
        DateTime? fromDate,
        DateTime? toDate,
        int? pageNumber,
        int? pageSize,
        IHttpClientFactory httpFactory,
        CancellationToken ct)
    {
        var q = new List<string>();
        if (status.HasValue) q.Add($"status={status.Value}");
        if (bookId.HasValue) q.Add($"bookId={bookId.Value}");
        if (roomId.HasValue) q.Add($"roomId={roomId.Value}");
        if (fromDate.HasValue) q.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) q.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        if (pageNumber.HasValue) q.Add($"pageNumber={pageNumber.Value}");
        if (pageSize.HasValue) q.Add($"pageSize={pageSize.Value}");

        var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
        return ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get,
            $"/api/borrow-requests/my{qs}", null, ct);
    }

    private static Task<IResult> GetAllAsync(
        Guid? userId,
        Guid? roomId,
        Guid? bookId,
        BorrowRequestStatus? status,
        string? userQuery,
        string? bookQuery,
        DateTime? fromDate,
        DateTime? toDate,
        int? pageNumber,
        int? pageSize,
        IHttpClientFactory httpFactory,
        CancellationToken ct)
    {
        var q = new List<string>();
        if (userId.HasValue) q.Add($"userId={userId.Value}");
        if (roomId.HasValue) q.Add($"roomId={roomId.Value}");
        if (bookId.HasValue) q.Add($"bookId={bookId.Value}");
        if (status.HasValue) q.Add($"status={status.Value}");
        if (!string.IsNullOrWhiteSpace(userQuery)) q.Add($"userQuery={Uri.EscapeDataString(userQuery)}");
        if (!string.IsNullOrWhiteSpace(bookQuery)) q.Add($"bookQuery={Uri.EscapeDataString(bookQuery)}");
        if (fromDate.HasValue) q.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) q.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        if (pageNumber.HasValue) q.Add($"pageNumber={pageNumber.Value}");
        if (pageSize.HasValue) q.Add($"pageSize={pageSize.Value}");

        var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
        return ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get,
            $"/api/borrow-requests{qs}", null, ct);
    }

    // Тело содержит только специфичные для действия поля: целевая заявка
    // однозначна из URL, BorrowRequestId в body перезаписывается на стороне WebAPI.
    private static Task<IResult> ApproveAsync(
        Guid id, ApproveBorrowRequest request,
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post,
            $"/api/borrow-requests/{id}/approve", request, ct);

    private static Task<IResult> RejectAsync(
        Guid id, RejectBorrowRequest request,
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post,
            $"/api/borrow-requests/{id}/reject", request, ct);

    private static Task<IResult> IssueAsync(
        Guid id, IssueBorrowRequest request,
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post,
            $"/api/borrow-requests/{id}/issue", request, ct);

    private static Task<IResult> CancelAsync(
        Guid id, CancelBorrowRequest request,
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post,
            $"/api/borrow-requests/{id}/cancel", request, ct);
}