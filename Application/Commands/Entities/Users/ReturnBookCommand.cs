namespace LibApp.Application.Commands.Entities.Users;

public class ReturnBookCommand(
    IUnitOfWork unitOfWork,
    BorrowingValidatorAsync validator,
    PenaltyCalculatorService penaltyCalculator,
    INotificationService notificationService,
    ICurrentUserService currentUser) : ICreateOrUpdateCommand<ReturnBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(ReturnBookRequest request, CancellationToken cancellationToken)
    {
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        // Валидация
        var userRoomBookId = new Id(request.UserRoomBookId);
        var userRoomBook = await userRoomBookRepo.FirstOrDefaultAsync(urb => urb.Id == userRoomBookId, cancellationToken);
        if (userRoomBook == null) throw new UserRoomBookNotFoundException();

        var validationResult = await validator.ValidateReturnAsync(userRoomBook, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Валидация списания — до транзакции, чтобы не начинать её впустую.
        if (request.DiscardReason.HasValue)
        {
            if (request.DiscardReason == DiscardReason.Loss
                && (!request.CompensationAmount.HasValue || request.CompensationAmount.Value <= 0))
            {
                throw new LibValidationException
                {
                    ExceptionDetails = ["При утрате книги необходимо указать сумму компенсации (> 0)"]
                };
            }

            if (request.CompensationAmount.HasValue && request.CompensationAmount.Value < 0)
            {
                throw new LibValidationException
                {
                    ExceptionDetails = ["Сумма компенсации не может быть отрицательной"]
                };
            }
        }

        // Получаем книгу
        var roomBook = userRoomBook.RoomBook;

        // Расчёт штрафа (если есть)
        var penalty = penaltyCalculator.CalculatePenaltyForReturn(userRoomBook);
        if (penalty.HasValue && penalty.Value > 0)
        {
            userRoomBook.Penalty = penalty;

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
            if (request.DiscardReason.HasValue)
            {
                // Списание: экземпляр уходит из фонда.
                roomBook.BookCount--;

                var discarded = new DiscardedBook
                {
                    Id = new Id(Guid.NewGuid()),
                    Book = roomBook.Book,
                    Room = roomBook.Room,
                    RoomId = roomBook.Room.Id,
                    Amount = 1,
                    DiscardedDate = DateTime.UtcNow,
                    DiscardReason = request.DiscardReason.Value,
                    ApprovedById = currentUser.LibraryUserId,
                    ApprovedBy = currentUser.FullName,
                    CompensationAmount = request.CompensationAmount
                };

                await discardedRepo.AddAsync(discarded, cancellationToken);
                await roomBookRepo.UpdateAsync(roomBook, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                var anyRemaining = await roomBookRepo.AnyAsync(
                    rb => rb.Book.Id == roomBook.Book.Id && rb.BookCount > 0,
                    cancellationToken);

                if (!anyRemaining)
                {
                    roomBook.Book.IsDiscarded = true;
                    await bookRepo.UpdateAsync(roomBook.Book, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                // Обычный возврат: BookCount не трогаем, книга остаётся в фонде.
                await roomBookRepo.UpdateAsync(roomBook, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var parts = new List<string> { "Книга возвращена" };
        if (request.DiscardReason.HasValue)
            parts.Add($"Списана ({ReasonName(request.DiscardReason.Value)})");
        if (userRoomBook.Penalty.HasValue && userRoomBook.Penalty.Value > 0)
            parts.Add($"Штраф: {userRoomBook.Penalty.Value:0.##} руб.");
        if (request.CompensationAmount.HasValue && request.CompensationAmount.Value > 0)
            parts.Add($"Компенсация: {request.CompensationAmount.Value:0.##} руб.");

        return ResponseFactory.Success(string.Join(". ", parts));
    }

    private static string ReasonName(DiscardReason r) => r switch
    {
        DiscardReason.Wear => "износ",
        DiscardReason.Damaged => "повреждение",
        DiscardReason.Loss => "утрата",
        DiscardReason.Obsolete => "устарела",
        _ => "другое"
    };
}