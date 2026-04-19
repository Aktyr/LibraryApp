namespace LibApp.Application.Commands.Entities.Users;
public class ReturnBookCommand : ICreateOrUpdateCommand<ReturnBookRequest, BasicCreateDeleteResponse>, ICommand
{
    private readonly IRepository<UserRoomBook> _userRoomBookRepo;
    private readonly IRepository<RoomBook> _roomBookRepo;
    private readonly BorrowingValidatorAsync _validator;
    private readonly PenaltyCalculatorService _penaltyCalculator;
    private readonly INotificationService _notificationService;

    public ReturnBookCommand(
        IRepository<UserRoomBook> userRoomBookRepo,
        IRepository<RoomBook> roomBookRepo,
        BorrowingValidatorAsync validator,
        PenaltyCalculatorService penaltyCalculator,
        INotificationService notificationService)
    {
        _userRoomBookRepo = userRoomBookRepo;
        _roomBookRepo = roomBookRepo;
        _validator = validator;
        _penaltyCalculator = penaltyCalculator;
        _notificationService = notificationService;
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


        // Расчёт штрафа (если есть)
        var penalty = _penaltyCalculator.CalculatePenaltyForReturn(userRoomBook);
        if (penalty.HasValue && penalty.Value > 0)
        {
            userRoomBook.Penalty = penalty;

            // Уведомление о штрафе
            await _notificationService.SendOverdueNotificationAsync(
                userRoomBook.User,
                userRoomBook,
                penalty.Value,
                cancellationToken);
        }

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