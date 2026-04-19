namespace LibApp.Core.Entities;

public class Room : IEntity
{
    public Id Id { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int SumOfBooks => RoomBooks.Select(x => x.BookCount).Sum();
    [InverseProperty(nameof(RoomBook.Room))] public virtual ICollection<RoomBook> RoomBooks { get; set; } = [];
    //public override string ToString() => $"Комната {Name}, Кол-во книг: {Books.Select(x => x.Count).Sum()}";
}
