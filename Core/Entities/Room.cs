namespace LibApp.Core.Entities;

public class Room : IEntity
{
    public Room()
    {
        Id = new Id(Guid.NewGuid());
    }

    public Id Id { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int SumOfBooks => RoomBooks.Select(x => x.BookCount).Sum();

    public ICollection<RoomBook> RoomBooks { get; set; } = [];
    //public override string ToString() => $"Комната {Name}, Кол-во книг: {Books.Select(x => x.Count).Sum()}";
}
