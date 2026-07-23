namespace LibApp.Application.Commands.Entities.Users;

public class ReturnBookCommand(
    IUnitOfWork unitOfWork,
    BorrowingValidatorAsync validator,
    PenaltyCalculatorService penaltyCalculator,
    INotificationService notificationService) : ICreateOrUpdateCommand<ReturnBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(ReturnBookRequest request, CancellationToken cancellationToken)
    {
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        // Валидация
        var userRoomBook = (await userRoomBookRepo.Get(urb => urb.Id.Value == request.UserRoomBookId, cancellationToken)).FirstOrDefault()
            ?? throw new UserRoomBookNotFoundException();

        var validationResult = await validator.ValidateReturnAsync(userRoomBook, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Получаем книгу
        var roomBook = userRoomBook.RoomBook;

        // Расчёт штрафа (если есть)
        var penalty = penaltyCalculator.CalculatePenaltyForReturn(userRoomBook);
        if (penalty.HasValue && penalty.Value > 0)
        {
            userRoomBook.Penalty = penalty;

            // Уведомление о штрафе
            await notificationService.SendOverdueNotificationAsync(
                userRoomBook.User, 
                userRoomBook, 
                penalty.Value, 
                cancellationToken);
        }

        // Обновляем данные
        roomBook.BorrowedCount--;
        userRoomBook.ReturnDate = DateTime.UtcNow;

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var message = userRoomBook.Penalty.HasValue
            ? $"Книга возвращена. Штраф: {userRoomBook.Penalty} руб."
            : "Книга возвращена";

        return ResponseFactory.Success(message);
    }
}