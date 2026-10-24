namespace BlazorWeb.Client.Services;

public interface IRoomApiClient
{
    Task<RoomResponse> GetAllRoomsAsync(CancellationToken ct = default);
    Task<RoomResponse> GetRoomByIdAsync(Guid id, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> UpdateRoomAsync(UpdateRoomRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> DeleteRoomAsync(Guid id, CancellationToken ct = default);
}