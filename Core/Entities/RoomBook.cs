namespace LibApp.Core.Entities;

public class RoomBook : IEntity
{
    public RoomBook()
    {
        Id = new Id(Guid.NewGuid());
    }

    public Id Id { get; set; }
    public Room Room { get; set; } = null!;
    public int BookCount { get; set; }
    public Book Book { get; set; } = null!;
}