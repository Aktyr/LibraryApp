namespace LibraryApp.Models;

public class RoomBook(Room room, Book book)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Room Room { get; set; } = room;
    public int BookCount { get; set; }
    public Book Book { get; set; } = book;
}