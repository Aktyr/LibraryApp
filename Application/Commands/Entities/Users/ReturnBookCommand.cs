namespace LibApp.Application.Commands.Entities.Users;

public class ReturnBookCommand(
    IUnitOfWork unitOfWork,
    BorrowingValidatorAsync validator,
    PenaltyCalculatorService penaltyCalculator,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    IAuditService auditService) : ICreateOrUpdateCommand<ReturnBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(ReturnBookRequest request, CancellationToken cancellationToken)
    {
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        // Валидация
        var userRoomBookId = new Id(request.UserRoomBookId);

        // Eager-load: User — для ФИО в аудите, RoomBook.Book/Room — для текста аудита
        // и списания. Без явных Include lazy-прокси даёт по SQL на каждой навигации.
        var userRoomBook = (await userRoomBookRepo.GetAsync(
            filter: urb => urb.Id == userRoomBookId,
            cancellationToken: cancellationToken,
            includePaths:
            [
                IncludePaths.UserRoomBook.User,
                IncludePaths.UserRoomBook.RoomBookBook,
                IncludePaths.UserRoomBook.RoomBookRoom
            ])).FirstOrDefault();

        if (userRoomBook == null) throw new UserRoomBookNotFoundException();

        var validationResult = await validator.ValidateReturnAsync(userRoomBook, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        var roomBook = userRoomBook.RoomBook;

        // Валидация списания — до транзакции, чтобы не начинать её впустую.
        // AvailableCount + 1: списываем возвращаемый экземпляр, который сейчас числится
        // в BorrowedCount. Политика проверяет «можно ли списать, если бы книга уже была в фонде».
        if (request.DiscardReason.HasValue)
        {
            var discardErrors = DiscardPolicy.ValidateDiscard(
                amount: 1,
                bookCount: roomBook.BookCount,
                availableCount: roomBook.AvailableCount + 1,
                compensation: request.CompensationAmount,
                reason: request.DiscardReason,
                approvedBy: currentUser.FullName);

            if (discardErrors.Count > 0)
                throw new LibValidationException { ExceptionDetails = discardErrors };
        }

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

        await unitOfWork.ExecuteInTransactionAsync(async () =>
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
                    ReaderId = userRoomBook.User.Id,
                    ReaderName = userRoomBook.User.FullName,
                    CompensationAmount = request.CompensationAmount
                };

                await discardedRepo.AddAsync(discarded, cancellationToken);
                await roomBookRepo.UpdateAsync(roomBook, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Если после списания не осталось ни одного экземпляра ни в одной комнате — помечаем книгу списанной
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

            // ФИО читателя — чтобы в AuditReport было видно, у кого списана книга.
            var auditDetails = request.DiscardReason.HasValue
                ? $"Возврат со списанием: «{roomBook.Book.Title}» ({ReasonName(request.DiscardReason.Value)}). Читатель: {userRoomBook.User.FullName}"
                : $"Возврат книги: «{roomBook.Book.Title}». Читатель: {userRoomBook.User.FullName}";

            await auditService.LogAsync(
                AuditActionType.Return,
                nameof(UserRoomBook),
                userRoomBook.Id.Value,
                details: auditDetails,
                ct: cancellationToken);
        }, cancellationToken);

        var parts = new List<string> { "Книга возвращена" };
        if (request.DiscardReason.HasValue)
            parts.Add($"Списана ({ReasonName(request.DiscardReason.Value)})");
        if (userRoomBook.Penalty.HasValue && userRoomBook.Penalty.Value > 0)
            parts.Add($"Штраф: {userRoomBook.Penalty.Value:0.##} руб.");
        if (request.CompensationAmount.HasValue && request.CompensationAmount.Value > 0)
            parts.Add($"Компенсация: {request.CompensationAmount.Value:0.##} руб.");

        return ResponseFactory.Success(string.Join(". ", parts));
    }

    // В тексте ответа причина идёт строчными (как часть фразы «Списана (износ)»),
    // а в бейджах/select — с заглавной. Поэтому здесь явно to lower.
    private static string ReasonName(DiscardReason r) => r.DisplayName().ToLowerInvariant();
}