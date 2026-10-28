public class DiscardedBook : IEntity
{
    public Id Id { get; set; }
    public virtual Book Book { get; set; } = null!;
    public virtual Room Room { get; set; } = null!;
    public Id RoomId { get; set; }
    public int Amount { get; set; } // Количество списанных экземпляров
    public DateTime DiscardedDate { get; set; }
    public DiscardReason DiscardReason { get; set; }
    public decimal? CompensationAmount { get; set; } // Сумма компенсации, если утрата/повреждение и тд

    /// <summary>
    /// Ссылка на library.User (nullable: пользователь может быть удалён).
    /// </summary>
    public Id? ApprovedById { get; set; }
    public virtual User? ApprovedByUser { get; set; }

    /// <summary>
    /// Снимок ФИО на момент списания. Заполняется при создании
    /// и не меняется, даже если пользователь потом удалён.
    /// </summary>
    public string? ApprovedBy { get; set; }
}