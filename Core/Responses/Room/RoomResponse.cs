namespace LibApp.Core.Responses.Room;

public record RoomResponse(string Status,
                          RoomDTO Room) : IGetResponse;
public record RoomDTO(Guid Id,
                      string Name,
                      ICollection<RoomBookDTO> RoomBooks);

public record RoomBookDTO(Guid Id,
                          Guid RoomId,
                          Guid BookId,
                          int BookCount);