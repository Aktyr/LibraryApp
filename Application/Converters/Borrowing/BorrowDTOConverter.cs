namespace LibApp.Application.Converters.Borrowing;

internal class BorrowDTOConverter : IConverter<UserRoomBook, BorrowedBookDTO>
{
    public BorrowedBookDTO ToDto(UserRoomBook entity)
    {
        return new BorrowedBookDTO(
            Id: entity.Id.Value,
            BookTitle: entity.RoomBook.Book.Title,
            BookAuthor: entity.RoomBook.Book.Author,
            RoomName: entity.RoomBook.Room.Name,
            Borrow: entity.BorrowDate,
            Deadline: entity.Deadline,
            ReturnDate: entity.ReturnDate,
            Penalty: entity.Penalty
        );
    }

    public UserRoomBook ToEntity(BorrowedBookDTO dto)
    {
        throw new NotSupportedException("Конвертация из DTO в сущность не поддерживается");
    }

}
