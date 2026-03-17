namespace LibApp.Core.Requests.Entities.Room;

public record CreateRoomRequest(string Name, IEnumerable<Guid> Books) : IAddOrUpdateRequest;