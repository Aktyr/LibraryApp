namespace LibApp.Application.Converters.Borrowing;

public class BorrowDTOConverter : IConverter<UserRoomBook, BorrowedBookDTO>
{
    private readonly PenaltyCalculatorService _penaltyCalculator;
    public BorrowDTOConverter(PenaltyCalculatorService penaltyCalculator)
    {
        _penaltyCalculator = penaltyCalculator;
    }

    public BorrowedBookDTO ToDto(UserRoomBook entity)
    {
        decimal? currentPenalty = entity.Penalty;

        // Если книга на руках и штраф еще не был зафиксирован в БД — считаем его на лету
        if (entity.ReturnDate == null && !currentPenalty.HasValue)
        {
            currentPenalty = _penaltyCalculator.CalculatePenalty(entity.Deadline);
        }

        return new BorrowedBookDTO(
            Id: entity.Id.Value,
            BookTitle: entity.RoomBook.Book.Title,
            BookAuthor: entity.RoomBook.Book.Author,
            RoomName: entity.RoomBook.Room.Name,
            Borrow: entity.BorrowDate,
            Deadline: entity.Deadline,
            ReturnDate: entity.ReturnDate,
            Penalty: currentPenalty,
            TotalExtendedDays: entity.TotalExtendedDays
        );
    }

    public UserRoomBook ToEntity(BorrowedBookDTO dto)
    {
        throw new NotSupportedException("Конвертация из DTO в сущность не поддерживается");
    }
}