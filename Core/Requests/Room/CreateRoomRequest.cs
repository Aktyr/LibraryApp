namespace LibApp.Core.Requests.Room;

public record CreateRoomRequest(string Name, IEnumerable<Guid> Books) : IAddOrUpdateRequest;