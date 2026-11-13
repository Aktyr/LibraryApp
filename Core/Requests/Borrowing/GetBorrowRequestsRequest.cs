namespace LibApp.Core.Requests.Borrowing;

/// <summary>
/// Список заявок с фильтрами и пагинацией. Один request на два эндпоинта:
/// <c>/api/borrow-requests/my</c> для читателя (UserId подставляется из токена)
/// и <c>/api/borrow-requests</c> для библиотекаря (доступен полный набор фильтров).
/// </summary>
public class GetBorrowRequestsRequest : IGetRequest
{
    public Guid? UserId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? BookId { get; set; }
    public BorrowRequestStatus? Status { get; set; }

    /// <summary>Поиск по ФИО читателя.</summary>
    public string? UserQuery { get; set; }

    /// <summary>Поиск по названию книги.</summary>
    public string? BookQuery { get; set; }

    /// <summary>Нижняя граница CreatedAt. Если задана только дата — с 00:00:00 UTC.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Верхняя граница CreatedAt. Дата трактуется как включительно до конца суток UTC.</summary>
    public DateTime? ToDate { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}