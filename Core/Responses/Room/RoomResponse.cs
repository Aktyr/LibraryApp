namespace LibApp.Core.Responses.Room;
public record RoomResponse(string Status, string Message, RoomDTO[] Room) : IGetResponse;
public record RoomDTO(Guid Id,
                      string Name,
                      ICollection<RoomBookDTO> RoomBook);

public record RoomBookDTO(Guid Id,
                          Guid RoomId,
                          Guid BookId,
                          int BookCount); // перенести в будущем