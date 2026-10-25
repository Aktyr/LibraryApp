namespace BlazorWeb.Client.Services;

public class RoomApiClient(HttpClient http) : IRoomApiClient
{
    private const string BasePath = "/api/proxy/rooms";

    public async Task<RoomResponse> GetAllRoomsAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<RoomResponse>(BasePath, ct)
           ?? new RoomResponse("Error", "Empty response", []);

    public async Task<RoomResponse> GetRoomByIdAsync(Guid id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<RoomResponse>($"{BasePath}/{id}", ct)
           ?? new RoomResponse("Error", "Empty response", []);

    public async Task<BasicCreateDeleteResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(BasePath, request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> UpdateRoomAsync(UpdateRoomRequest request, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync(BasePath, request, ct);
        return await ReadAsync(resp, ct);
    }

    public async Task<BasicCreateDeleteResponse> DeleteRoomAsync(Guid id, CancellationToken ct = default)
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
}