using BlazorWeb.Client.Services.Interfaces;

namespace BlazorWeb.Client.Services.APIs;

public class BorrowingApiClient(HttpClient http) : IBorrowingApiClient
{
    private const string BasePath = "/api/proxy/borrowing";

    public async Task<BasicCreateDeleteResponse> BorrowBookAsync(BorrowBookRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{BasePath}/borrow", request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> ReturnBookAsync(ReturnBookRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{BasePath}/return", request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> ExtendDeadlineAsync(ExtendDeadlineRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{BasePath}/extend", request, ct);
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
}