namespace LibApp.Core.DTO;

public record UserRoomBookDTO(Guid Id,
                              DateTime Issue,
                              DateTime? Deadline,
                              Guid UserId,
                              Guid RoomBookId)
{ public UserRoomBookDTO() : this(default, default, default, default, default) { } }
