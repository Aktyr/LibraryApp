namespace LibApp.Core.DTO.Entities;

public record UserRoomBookDTO(Guid Id,
                              DateTime Borrow,
                              DateTime? Deadline,
                              Guid UserId,
                              Guid RoomBookId)
{ public UserRoomBookDTO() : this(default, default, default, default, default) { } }
