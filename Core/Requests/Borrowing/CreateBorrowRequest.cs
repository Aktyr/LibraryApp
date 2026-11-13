namespace LibApp.Core.Requests.Borrowing;

/// <summary>
/// Запрос читателя на выдачу экземпляра книги из комнаты.
/// <see cref="RoomBookId"/> — идентификатор конкретной связки RoomBook, а не книги каталога:
/// у одной книги может быть несколько экземпляров в разных комнатах.
/// </summary>
public class CreateBorrowRequest : IAddOrUpdateRequest
{
    public Guid RoomBookId { get; set; }
}