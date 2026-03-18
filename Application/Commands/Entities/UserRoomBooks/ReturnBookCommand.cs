namespace LibApp.Application.Commands.Entities.UserRoomBooks;
public class ReturnBookCommand : ICreateOrUpdateCommand<ReturnBookRequest, BasicCreateDeleteResponse>
{
    private readonly IRepository<UserRoomBook> _userRoomBookRepo;
    private readonly IRepository<RoomBook> _roomBookRepo;
    private readonly BorrowingValidatorAsync _validator;

    public ReturnBookCommand(
        IRepository<UserRoomBook> userRoomBookRepo,
        IRepository<RoomBook> roomBookRepo,
        BorrowingValidatorAsync validator)
    {
        _userRoomBookRepo = userRoomBookRepo;
        _roomBookRepo = roomBookRepo;
        _validator = validator;
    }

    public async Task<BasicCreateDeleteResponse> Execute(ReturnBookRequest request, CancellationToken cancellationToken)
    {
        // Валидация 
        var userRoomBook = (await _userRoomBookRepo.Get(urb => urb.Id.Value == request.UserRoomBookId, cancellationToken)).FirstOrDefault()
            ?? throw new UserRoomBookNotFoundException();

        var validationResult = await _validator.ValidateReturnAsync(userRoomBook, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // Получаем книгу
        var roomBook = userRoomBook.RoomBook;

        // Рассчёт штрафа при просрочке 
        /*if (userRoomBook.Deadline < DateTime.Now)
        {
            decimal rubPerDay = 10;
            var daysOverdue = (DateTime.Now - userRoomBook.Deadline.Value).Days;
            userRoomBook.Penalty = daysOverdue * rubPerDay;
        }*/

        // Обновляем данные
        userRoomBook.ReturnDate = DateTime.Now;
        roomBook.BorrowedCount--;

        await _userRoomBookRepo.Update(userRoomBook, cancellationToken);
        await _roomBookRepo.Update(roomBook, cancellationToken);

        var message = userRoomBook.Penalty.HasValue
            ? $"Книга возвращена. Штраф: {userRoomBook.Penalty} руб."
            : "Книга возвращена";

        return new BasicCreateDeleteResponse("Ok", message);
    }
}