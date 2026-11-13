namespace LibApp.Core.Entities;

public class UserRoomBook : IEntity
{
    public Id Id { get; set; }
    public DateTime BorrowDate { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }
    public DateTime? ReturnDate { get; set; }    // Фактическая дата возврата (для закрытия Книговыдачи)
    public int TotalExtendedDays { get; set; }
    public bool IsReturned => ReturnDate.HasValue;
    public decimal? Penalty { get; set; }

    /// <summary>
    /// Дата отправки напоминания о приближении срока возврата. Заполняется фоновым
    /// DeadlineCheckService. Повторно не отправляется: при рестарте приложения
    /// или следующем цикле проверки поле уже не null.
    /// </summary>
    public DateTime? ReminderSentAt { get; set; }

    /// <summary>
    /// Дата отправки уведомления о просрочке. Заполняется фоновым DeadlineCheckService
    /// один раз, при первом обнаружении просрочки. Дальнейший рост штрафа
    /// виден в профиле пользователя, письма не дублируются.
    /// </summary>
    public DateTime? OverdueNoticeSentAt { get; set; }


    public Id UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Id RoomBookId { get; set; }
    public virtual RoomBook RoomBook { get; set; } = null!;

    /// <summary>
    /// Заявка, по которой создана эта выдача. null — выдача оформлена библиотекарем напрямую,
    /// без предварительной заявки читателя. Устанавливается только при одобрении заявки.
    /// </summary>
    public Id? BorrowRequestId { get; set; }
    public virtual BorrowRequest? BorrowRequest { get; set; }

    /// <summary>Оплаты штрафа. По правилу — не более одной (уникальный индекс в конфиге EF).</summary>
    public virtual ICollection<PenaltyPayment> PenaltyPayments { get; set; } = [];

    /// <summary>Штраф полностью оплачен. Вычисляется по наличию платежа; в БД не хранится.</summary>
    public bool IsPenaltyPaid => PenaltyPayments.Count > 0;
}