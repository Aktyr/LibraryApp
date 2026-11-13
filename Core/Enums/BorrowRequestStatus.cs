namespace LibApp.Core.Enums;

public enum BorrowRequestStatus
{
    /// <summary>Создана, ждёт рассмотрения библиотекарем.</summary>
    Pending,

    /// <summary>Одобрена, бронь активна до BorrowRequest.ExpiresAt. Книга ещё не выдана.</summary>
    Approved,

    /// <summary>Библиотекарь отклонил. Причина — в BorrowRequest.Comment.</summary>
    Rejected,

    /// <summary>Читатель отменил сам, из Pending или Approved.</summary>
    Cancelled,

    /// <summary>Бронь истекла: читатель не забрал книгу до ExpiresAt.</summary>
    Expired,

    /// <summary>Фактическая выдача оформлена, PickedUpAt заполнен. Закрывается возвратом книги.</summary>
    Completed
}