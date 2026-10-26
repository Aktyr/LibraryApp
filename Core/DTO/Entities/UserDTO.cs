namespace LibApp.Core.DTO.Entities;

public record UserDTO(Guid Id,
                      string LastName,
                      string FirstName,
                      string MiddleName,
                      string ContactInfo,
                      TimeSpan? NearestReturnTimeSpan,
                      ICollection<UserRoomBookDTO> RoomBooks,
                      UserRole Role = UserRole.Reader,
                      int ActiveBorrowedCount = 0,
                      string Email = "")
{ public UserDTO() : this(default, default!, default!, default!, default!, default, default!) { } }