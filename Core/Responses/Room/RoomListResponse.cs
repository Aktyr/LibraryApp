namespace LibApp.Core.Responses.Room;

public record RoomsListResponse(string Status, RoomDTO[] Rooms) : IGetResponse;

