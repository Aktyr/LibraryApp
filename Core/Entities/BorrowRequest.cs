namespace LibApp.Core.Entities;

/// <summary>
/// Заявка читателя на выдачу экземпляра книги из комнаты. Проходит путь
/// Pending → Approved (бронь активна) → Completed (фактическая выдача) либо
/// Pending → Rejected / Cancelled, Approved → Cancelled / Expired.
/// Связь с фактической выдачей — через UserRoomBook.BorrowRequestId, заполняется в момент выдачи.
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

    /// <summary>
    /// Время решения библиотекаря: Approve или Reject. Для остальных статусов — null.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Библиотекарь или администратор, принявший решение по заявке (Approve/Reject).</summary>
    public Id? ProcessedByUserId { get; set; }
    public virtual User? ProcessedByUser { get; set; }

    /// <summary>
    /// Срок действия брони. Заполняется при Approve: ProcessedAt + BorrowingSettings.ReserveHoldHours.
    /// После ExpiresAt фоновый сервис переводит заявку в Expired. Для остальных статусов — null.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Фактическая дата выдачи книги читателю. Заполняется в момент оформления выдачи
    /// по брони (IssueBorrowRequestCommand). Для Completed — не null.
    /// </summary>
    public DateTime? PickedUpAt { get; set; }

    /// <summary>
    /// Комментарий сотрудника. Для Rejected обязателен (см. RejectBorrowRequestCommand),
    /// для остальных статусов опционален.
    /// </summary>
    public string? Comment { get; set; }
}