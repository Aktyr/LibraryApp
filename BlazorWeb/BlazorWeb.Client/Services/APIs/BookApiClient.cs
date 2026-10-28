using BlazorWeb.Client.Services.Interfaces;

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
}