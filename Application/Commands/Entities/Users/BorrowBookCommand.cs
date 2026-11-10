namespace LibApp.Application.Commands.Entities.Users;

public class BorrowBookCommand(
    IUnitOfWork unitOfWork,
    BorrowingValidatorAsync validator,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<BorrowBookRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        BorrowBookRequest request, CancellationToken cancellationToken)
    {
        var userRepo = UnitOfWork.GetRepository<User>();
        var roomBookRepo = UnitOfWork.GetRepository<RoomBook>();
        var userRoomBookRepo = UnitOfWork.GetRepository<UserRoomBook>();

        // Валидация
        var userId = new Id(request.UserId);
        var roomBookId = new Id(request.RoomBookId);

        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) throw new UserNotFoundException();

        var roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Id == roomBookId, cancellationToken);
        if (roomBook == null) throw new UserRoomBookNotFoundException();

        if (await userRoomBookRepo.AnyAsync(urb => urb.User.Id == userId
                                                && urb.RoomBook.Id == roomBookId
                                                && urb.ReturnDate == null, cancellationToken))
            throw new LibValidationException { ExceptionDetails = ["Пользователь уже взял эту книгу и ещё не вернул её"] };

        var validationResult = await validator.ValidateBorrowAsync(user, roomBook, request.BorrowDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            RoomBook = roomBook,
            BorrowDate = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(request.BorrowDays)
        };

        roomBook.BorrowedCount++;
        user.RoomBooks.Add(userRoomBook);

        // SaveChanges и LogAsync выполняет база в одной транзакции.
        var audit = new AuditEntry(
            AuditActionType.Borrow,
            nameof(UserRoomBook),
            userRoomBook.Id.Value,
            $"Выдана книга «{roomBook.Book.Title}» пользователю «{user.FullName}». Срок: {userRoomBook.Deadline:d}");

        return (ResponseFactory.Success($"Книга выдана. Срок возврата: {userRoomBook.Deadline:d}"), audit);
    }
}