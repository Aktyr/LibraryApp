namespace LibApp.Core.Responses.Room;

public record RoomsListResponse(string Status, string Message, RoomDTO[] Rooms) : IGetResponse;

