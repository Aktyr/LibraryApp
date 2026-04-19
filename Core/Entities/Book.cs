namespace LibApp.Core.Entities;

public class Book : IEntity
{
    public Id Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Publisher { get; set; } = string.Empty;
    [InverseProperty(nameof(Entities.RoomBook.Book))] public virtual ICollection<RoomBook> RoomBook { get; set; } = [];

    public override string ToString() =>
        $"Название: {Title}\nАвтор: {Author}\nГод: {Year}";
}