namespace LibApp.Core.Entities;

public class RoomBook : IEntity
{
    public Id Id { get; set; }
    public Room Room { get; set; } = null!;
    public int BookCount { get; set; }          // Общее количество
    public int BorrowedCount { get; set; }      // Количество выданных
    public Book Book { get; set; } = null!;

    // Вычисляемое поле - доступно для выдачи
    public int AvailableCount => BookCount - BorrowedCount;

}