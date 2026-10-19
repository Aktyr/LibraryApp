namespace BlazorWeb.Client.Services;

public class BookApiClient(HttpClient http) : IBookApiClient
{
    public async Task<BookResponse> GetAllBooksAsync(CancellationToken ct = default)
    {
        // Обратите внимание: WebAPI возвращает BookResponse
        var response = await http.GetFromJsonAsync<BookResponse>("/api/books", ct);
        return response ?? new BookResponse("Error", "Empty response", []);
    }

    public async Task<BasicCreateDeleteResponse> CreateBookAsync(CreateBookRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/books", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BasicCreateDeleteResponse>(ct)
               ?? new BasicCreateDeleteResponse("Error", "Empty response");
    }
}