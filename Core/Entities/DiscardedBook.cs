namespace LibApp.Core.Entities;

public class DiscardedBook : IEntity
{
    public Id Id { get; set; }
    public virtual Book Book { get; set; } = null!;
    public virtual Room Room { get; set; } = null!;
    public Id RoomId { get; set; }

    public int Amount { get; set; }                     // Количество списанных экземпляров
    public DateTime DiscardedDate { get; set; }
    public DiscardReason DiscardReason { get; set; }
    public string? ApprovedBy { get; set; }             // Кто списал
    public decimal? CompensationAmount { get; set; }    // Сумма компенсации, если утрата
}
