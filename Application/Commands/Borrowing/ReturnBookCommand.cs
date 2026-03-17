namespace LibApp.Application.Commands.Borrowing;
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
        // 1. Получаем запись о выдаче
        var userRoomBook = (await _userRoomBookRepo.Get(urb => urb.Id.Value == request.UserRoomBookId, cancellationToken)).FirstOrDefault()
            ?? throw new Exception("Запись о выдаче не найдена");

        // 2. Валидация
        var validationResult = await _validator.ValidateReturnAsync(userRoomBook, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // 3. Получаем связанную книгу
        var roomBook = userRoomBook.RoomBook;

        /*// 4. Рассчитываем штраф (если просрочка)
        if (userRoomBook.Deadline < DateTime.Now)
        {
            var daysOverdue = (DateTime.Now - userRoomBook.Deadline.Value).Days;
            userRoomBook.Penalty = daysOverdue * 10; // 10 руб/день
        }*/

        // 5. Обновляем данные
        userRoomBook.ReturnDate = DateTime.Now;
        roomBook.BorrowedCount--;

        // 6. Сохраняем
        await _userRoomBookRepo.Update(userRoomBook, cancellationToken);
        await _roomBookRepo.Update(roomBook, cancellationToken);

        var message = userRoomBook.Penalty.HasValue
            ? $"Книга возвращена. Штраф: {userRoomBook.Penalty} руб."
            : "Книга возвращена";

        return new BasicCreateDeleteResponse("Ok", message);
    }
}