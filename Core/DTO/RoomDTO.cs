namespace LibApp.Core.DTO;

public record RoomDTO(Guid Id,
                      string Name,
                      ICollection<RoomBookDTO> RoomBook)
{ public RoomDTO() : this(default, default!, default!) { } }
