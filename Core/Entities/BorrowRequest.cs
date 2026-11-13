namespace LibApp.Core.Entities;

/// <summary>
/// Заявка читателя на выдачу экземпляра книги из комнаты. Создаётся в статусе Pending
/// и переходит в Approved/Rejected после решения библиотекаря. В Completed переводится
/// при возврате связанной выдачи (см. UserRoomBook.BorrowRequestId).
/// </summary>
public class BorrowRequest : IEntity
{
    public Id Id { get; set; }

    public Id UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Id RoomBookId { get; set; }
    public virtual RoomBook RoomBook { get; set; } = null!;

    public BorrowRequestStatus Status { get; set; } = BorrowRequestStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    /// <summary>Библиотекарь или администратор, принявший решение по заявке.</summary>
    public Id? ProcessedByUserId { get; set; }
    public virtual User? ProcessedByUser { get; set; }

    /// <summary>
    /// Комментарий сотрудника при обработке. Для Rejected обязателен (см. BorrowRequestPolicy),
    /// для Approved опционален: причина одобрения не нужна, но иногда полезно оставить пояснение.
    /// </summary>
    public string? Comment { get; set; }
}