namespace LibraryApp.Models;

public class UserRoomBook(User user, RoomBook roomBook)
{
    public DateTime Issue { get; set; } = DateTime.Today;
    public DateTime Deadline { get; set; } = DateTime.Today.AddDays(14);
    public User User { get; set; } = user;
    public RoomBook RoomBook { get; set; } = roomBook;

}
