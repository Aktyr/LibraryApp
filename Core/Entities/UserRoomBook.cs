namespace LibApp.Core.Entities;

public class UserRoomBook : IEntity
{
    public Id Id { get; set; }
    public DateTime BorrowDate { get; set; } = DateTime.Now;
    public DateTime? Deadline { get; set; }
    public DateTime? ReturnDate { get; set; }    // Фактическая дата возврата (для закрытия Книговыдачи)
    public bool IsReturned => ReturnDate.HasValue;
    public decimal? Penalty { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual RoomBook RoomBook { get; set; } = null!;
}