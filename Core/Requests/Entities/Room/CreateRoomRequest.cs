namespace LibApp.Core.Requests.Entities.Room;

// todo зачем тут IEnumerable<Guid> Books
public record CreateRoomRequest(string Name, IEnumerable<Guid> Books) : IAddOrUpdateRequest;