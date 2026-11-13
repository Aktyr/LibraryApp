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
    }

    // Создание заявки читателем
    // UserId подставит WebAPI из JWT; в теле его нет.
    private static Task<IResult> CreateAsync(
        CreateBorrowRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post,
            "/api/borrow-requests", request, ct);

    // Мои заявки
    // userId из query игнорируется: WebAPI берёт его из JWT.
    // BFF пробрасывает остальные фильтры (status, bookId, roomId, даты, пагинация),
    // но не userId — иначе клиент мог бы подменить цель на /my.
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

    // Список для библиотекаря 
    // userId проходит через прокси — это легитимно для страницы /users/{id}.
    // Доступ к общему эндпоинту закрыт на WebAPI ролью Librarian/Admin,
    // прокси только повторяет `RequireAuthorization` выше.
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

    // Одобрение 
    // id из route перезаписывает BorrowRequestId в теле на стороне WebAPI.
    // Тело содержит только BorrowDays — целевая заявка однозначна из URL.
    private static Task<IResult> ApproveAsync(
        Guid id, ApproveBorrowRequest request,
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post,
            $"/api/borrow-requests/{id}/approve", request, ct);

    // Отклонение
    private static Task<IResult> RejectAsync(
        Guid id, RejectBorrowRequest request,
        IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post,
            $"/api/borrow-requests/{id}/reject", request, ct);
}