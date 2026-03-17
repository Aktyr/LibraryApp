namespace LibApp.Core.Entities;

public class UserRoomBook : IEntity
{
    public UserRoomBook()
    {
        Id = new Id(Guid.NewGuid());
    }

    public Id Id { get; set; }
    public DateTime BorrowDate { get; set; } = DateTime.Now;
    public DateTime? Deadline { get; set; }
    public DateTime? ReturnDate { get; set; }    // Фактическая дата возврата (для закрытия Книговыдачи)
    public bool IsReturned => ReturnDate.HasValue;
    public decimal? Penalty { get; set; }


    public User User { get; set; } = null!;
    public RoomBook RoomBook { get; set; } = null!;
}