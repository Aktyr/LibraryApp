namespace LibApp.Core.Responses.Room;
public record RoomResponse(string Status, string Message, RoomDTO[] Room) : IGetResponse;
