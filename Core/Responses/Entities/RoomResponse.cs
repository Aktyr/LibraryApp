namespace LibApp.Core.Responses.Entities;
public record RoomResponse(string Status, string Message, RoomDTO[] Room) : IGetResponse;
