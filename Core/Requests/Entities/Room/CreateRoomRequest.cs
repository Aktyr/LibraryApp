namespace LibApp.Core.Requests.Entities.Room;

// fixme зачем в создании комнаты IEnumerable<Guid> Books
public record CreateRoomRequest(string Name, IEnumerable<Guid> Books) : IAddOrUpdateRequest;