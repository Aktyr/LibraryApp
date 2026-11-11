namespace LibApp.Core.Entities;

public class RoomBook : IEntity
{
    public Id Id { get; set; }
    public int BookCount { get; set; }          // Общее количество
    public int BorrowedCount { get; set; }      // Количество выданных

    public Id RoomId { get; set; }
    public virtual Room Room { get; set; } = null!;

    public Id BookId { get; set; }
    public virtual Book Book { get; set; } = null!;

    // Вычисляемое поле - доступно для выдачи
    public int AvailableCount => BookCount - BorrowedCount;
}