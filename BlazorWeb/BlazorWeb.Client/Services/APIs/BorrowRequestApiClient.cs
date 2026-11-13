namespace BlazorWeb.Client.Services.APIs;

public class BorrowRequestApiClient(HttpClient http) : IBorrowRequestApiClient
{
    private const string BasePath = "/api/proxy/borrow-requests";

    public async Task<BasicCreateDeleteResponse> CreateAsync(
        CreateBorrowRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(BasePath, request, ct);
        return await ReadAsync(resp, ct);
    }

    // /my не принимает userId в query — сервер берёт его из JWT.
    // Поэтому BuildQuery с includeUserId: false.
    public async Task<BorrowRequestResponse> GetMyAsync(
        GetBorrowRequestsRequest request, CancellationToken ct = default)
    {
        var qs = BuildQuery(request, includeUserId: false);
        return await http.GetFromJsonAsync<BorrowRequestResponse>($"{BasePath}/my{qs}", ct)
               ?? new BorrowRequestResponse("Error", "Empty response", []);
    }

    public async Task<BorrowRequestResponse> GetAllAsync(
        GetBorrowRequestsRequest request, CancellationToken ct = default)
    {
        var qs = BuildQuery(request, includeUserId: true);
        return await http.GetFromJsonAsync<BorrowRequestResponse>($"{BasePath}{qs}", ct)
               ?? new BorrowRequestResponse("Error", "Empty response", []);
    }

    // id передаётся только в route: сервер перезаписывает request.BorrowRequestId из id.
    // В теле — только специфичные для действия поля, чтобы клиент не мог подменить цель.
    public async Task<BasicCreateDeleteResponse> ApproveAsync(
        Guid borrowRequestId, ApproveBorrowRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(
            $"{BasePath}/{borrowRequestId}/approve", request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> RejectAsync(
        Guid borrowRequestId, RejectBorrowRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(
            $"{BasePath}/{borrowRequestId}/reject", request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> IssueAsync(
        Guid borrowRequestId, IssueBorrowRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(
            $"{BasePath}/{borrowRequestId}/issue", request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> CancelAsync(
        Guid borrowRequestId, CancelBorrowRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(
            $"{BasePath}/{borrowRequestId}/cancel", request, ct);
        return await ReadAsync(resp, ct);
    }

    private static async Task<BasicCreateDeleteResponse> ReadAsync(
        HttpResponseMessage resp, CancellationToken ct)
    {
        if (!resp.IsSuccessStatusCode)
            throw await ApiErrorParser.ParseAsync(resp, ct);

        return await resp.Content.ReadFromJsonAsync<BasicCreateDeleteResponse>(ct)
               ?? new BasicCreateDeleteResponse("Error", "Empty response");
    }

    private static string BuildQuery(GetBorrowRequestsRequest request, bool includeUserId)
    {
        var q = new List<string>();

        if (includeUserId && request.UserId.HasValue)
            q.Add($"userId={request.UserId.Value}");
        if (request.RoomId.HasValue)
            q.Add($"roomId={request.RoomId.Value}");
        if (request.BookId.HasValue)
            q.Add($"bookId={request.BookId.Value}");
        if (request.Status.HasValue)
            q.Add($"status={request.Status.Value}");
        if (!string.IsNullOrWhiteSpace(request.UserQuery))
            q.Add($"userQuery={Uri.EscapeDataString(request.UserQuery)}");
        if (!string.IsNullOrWhiteSpace(request.BookQuery))
            q.Add($"bookQuery={Uri.EscapeDataString(request.BookQuery)}");
        if (request.FromDate.HasValue)
            q.Add($"fromDate={request.FromDate.Value:yyyy-MM-dd}");
        if (request.ToDate.HasValue)
            q.Add($"toDate={request.ToDate.Value:yyyy-MM-dd}");
        if (request.PageNumber > 0)
            q.Add($"pageNumber={request.PageNumber}");
        if (request.PageSize > 0)
            q.Add($"pageSize={request.PageSize}");

        return q.Count > 0 ? "?" + string.Join("&", q) : "";
    }
}