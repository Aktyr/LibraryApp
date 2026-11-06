namespace LibApp.Core.Entities;

/// <summary>
/// Принятая оплата штрафа за просрочку. Append-only: записи не редактируются,
/// при ошибке кассира — отмена платежа удалением (отдельная операция, аудит Delete).
/// На один UserRoomBook — не более одной оплаты (уникальный индекс).
/// </summary>
public class PenaltyPayment : IEntity
{
    public Id Id { get; set; }

    public Id UserRoomBookId { get; set; }
    public virtual UserRoomBook UserRoomBook { get; set; } = null!;

    /// <summary>Сумма оплаты. Всегда равна UserRoomBook.Penalty (частичные оплаты не поддерживаются).</summary>
    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; }

    /// <summary>Кассир, принявший оплату (Librarian или Admin).</summary>
    public Id PaidByUserId { get; set; }
    public virtual User PaidByUser { get; set; } = null!;

    /// <summary>Свободное примечание кассира (например, номер квитанции).</summary>
    public string? Notes { get; set; }
}