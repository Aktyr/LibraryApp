namespace BlazorWeb.Client.Services.APIs;

public class BookApiClient(HttpClient http) : IBookApiClient
{
    private const string BasePath = "/api/proxy/books";

    public async Task<BookResponse> GetAllBooksAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<BookResponse>(BasePath, ct)
           ?? new BookResponse("Error", "Empty response", []);

    public async Task<BookResponse> GetBookByIdAsync(Guid id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<BookResponse>($"{BasePath}/{id}", ct)
           ?? new BookResponse("Error", "Empty response", []);

    public async Task<BookResponse> GetBooksByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{BasePath}/batch", new { Ids = ids }, ct);

        if (!resp.IsSuccessStatusCode)
            throw await ApiErrorParser.ParseAsync(resp, ct);

        return await resp.Content.ReadFromJsonAsync<BookResponse>(ct)
               ?? new BookResponse("Error", "Empty response", []);
    }

    public async Task<BasicCreateDeleteResponse> CreateBookAsync(CreateBookRequest request, CancellationToken ct = default)
        => await PostAsync<CreateBookRequest>(request, ct);

    public async Task<BasicCreateDeleteResponse> UpdateBookAsync(UpdateBookRequest request, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync(BasePath, request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> DeleteBookAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"{BasePath}/{id}", ct);
        return await ReadAsync(resp, ct);
    }

    private async Task<BasicCreateDeleteResponse> PostAsync<TBody>(TBody body, CancellationToken ct)
    {
        var resp = await http.PostAsJsonAsync(BasePath, body, ct);
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
    public async Task<BasicCreateDeleteResponse> DiscardBookAsync(DiscardBookRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{BasePath}/discard", request, ct);
        return await ReadAsync(resp, ct);
    }
    public async Task<BookResponse> SearchBooksAsync(SearchBooksRequest request, CancellationToken ct = default)
    {
        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Query)) q.Add($"query={Uri.EscapeDataString(request.Query)}");
        if (!string.IsNullOrWhiteSpace(request.Title)) q.Add($"title={Uri.EscapeDataString(request.Title)}");
        if (!string.IsNullOrWhiteSpace(request.Author)) q.Add($"author={Uri.EscapeDataString(request.Author)}");
        if (!string.IsNullOrWhiteSpace(request.Genre)) q.Add($"genre={Uri.EscapeDataString(request.Genre)}");
        if (!string.IsNullOrWhiteSpace(request.Publisher)) q.Add($"publisher={Uri.EscapeDataString(request.Publisher)}");
        if (request.YearFrom.HasValue) q.Add($"yearFrom={request.YearFrom.Value}");
        if (request.YearTo.HasValue) q.Add($"yearTo={request.YearTo.Value}");
        if (request.AvailableOnly) q.Add("availableOnly=true");
        if (!string.IsNullOrWhiteSpace(request.SortBy)) q.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
        if (request.SortDescending) q.Add("sortDescending=true");

        // Пагинация
        if (request.PageNumber > 0) q.Add($"pageNumber={request.PageNumber}");
        if (request.PageSize > 0) q.Add($"pageSize={request.PageSize}");

        var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
        return await http.GetFromJsonAsync<BookResponse>($"{BasePath}/search{qs}", ct)
               ?? new BookResponse("Error", "Empty response", []);
    }
    public async Task<DiscardedBookResponse> GetDiscardedBooksAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        DiscardReason? discardReason = null,
        Guid? roomId = null,
        Guid? approvedById = null,
        Guid? bookId = null,
        CancellationToken ct = default)
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

        return await http.GetFromJsonAsync<DiscardedBookResponse>(
                   $"{BasePath}/discarded{request.BuildQueryString()}", ct)
               ?? new DiscardedBookResponse("Error", "Empty response", []);
    }
}