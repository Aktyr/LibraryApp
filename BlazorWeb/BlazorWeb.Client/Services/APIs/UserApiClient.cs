namespace BlazorWeb.Client.Services.APIs;

public class UserApiClient(HttpClient http) : IUserApiClient
{
    private const string BasePath = "/api/proxy/users";

    public async Task<UserResponse> GetAllUsersAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<UserResponse>(BasePath, ct)
           ?? new UserResponse("Error", "Empty response", []);

    public async Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<UserResponse>($"{BasePath}/{id}", ct)
           ?? new UserResponse("Error", "Empty response", []);

    public async Task<BorrowResponse> GetUserBooksAsync(Guid id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<BorrowResponse>($"{BasePath}/{id}/books", ct)
           ?? new BorrowResponse("Error", "Empty response", []);

    public async Task<BasicCreateDeleteResponse> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(BasePath, request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> UpdateUserAsync(UpdateUserRequest request, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync(BasePath, request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"{BasePath}/{id}", ct);
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
    public async Task<BasicCreateDeleteResponse> ChangeStatusAsync(Guid id, LibApp.Core.Enums.SubscriptionStatus newStatus, CancellationToken ct = default)
    {
        var req = new ChangeSubscriptionStatusRequest { NewStatus = newStatus };
        var resp = await http.PutAsJsonAsync($"{BasePath}/{id}/status", req, ct);
        return await ReadAsync(resp, ct);
    }
    public async Task<UserResponse> GetMeAsync(CancellationToken ct = default)
    => await http.GetFromJsonAsync<UserResponse>($"{BasePath}/my-profile", ct)
       ?? new UserResponse("Error", "Empty response", []);

    public async Task<BorrowResponse> GetMyBooksAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<BorrowResponse>($"{BasePath}/my-profile/books", ct)
           ?? new BorrowResponse("Error", "Empty response", []);
}