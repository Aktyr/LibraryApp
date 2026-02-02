namespace LibApp.Core.Entities;

public class UserRoomBook : IEntity
{
    public UserRoomBook()
    {
        Id = new Id(Guid.NewGuid());
    }

    public Id Id { get; set; }
    public DateTime Issue { get; set; } = DateTime.Now;
    public DateTime? Deadline { get; set; }
    public User User { get; set; } = null!;
    public RoomBook RoomBook { get; set; } = null!;
}