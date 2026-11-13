namespace LibApp.Core.Entities;

public class RoomBook : IEntity
{
    public Id Id { get; set; }
    public int BookCount { get; set; }          // Общее количество
    public int BorrowedCount { get; set; }      // Количество выданных

    /// <summary>
    /// Количество экземпляров, забронированных по одобренным заявкам
    /// (BorrowRequest.Status = Approved), но ещё не выданных читателю.
    /// Инкрементится в ApproveBorrowRequestCommand, декрементится в Issue/Cancel/Expire.
    /// </summary>
    public int ReservedCount { get; set; }

    public Id RoomId { get; set; }
    public virtual Room Room { get; set; } = null!;

    public Id BookId { get; set; }
    public virtual Book Book { get; set; } = null!;

    // Доступно для новых заявок и выдач: не выдано и не забронировано.
    public int AvailableCount => BookCount - BorrowedCount - ReservedCount;
}