namespace LibraryApp.Models;

public class Room(string name)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = name;

    public List<RoomBook> Books { get; set; }
    //public override string ToString() => $"Комната {Name}, Кол-во книг: {Books.Select(x => x.Count).Sum()}";
}
