namespace LibApp.Core.DTO;

public record RoomBookDTO(Guid Id,
                          Guid RoomId,
                          Guid BookId,
                          int BookCount)
{ public RoomBookDTO() : this(default, default, default, default!) { } }