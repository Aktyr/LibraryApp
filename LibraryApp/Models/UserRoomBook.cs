namespace LibraryApp.Models;

public class UserRoomBook(User user, RoomBook roomBook, DateTime? deadline)
{
    public DateTime Issue { get; set; } = DateTime.Today;
    public DateTime? Deadline { get; set; } = deadline;
    public User User { get; set; } = user;
    public RoomBook RoomBook { get; set; } = roomBook;

}
