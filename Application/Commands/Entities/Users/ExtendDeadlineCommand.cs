namespace LibApp.Application.Commands.Entities.Users;

public class ExtendDeadlineCommand(
    IUnitOfWork unitOfWork,
    BorrowingValidatorAsync validator,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<ExtendDeadlineRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        ExtendDeadlineRequest request, CancellationToken cancellationToken)
    {
        var userRoomBookRepo = UnitOfWork.GetRepository<UserRoomBook>();

        // Валидация
        var userRoomBookId = new Id(request.UserRoomBookId);
        var userRoomBook = await userRoomBookRepo.FirstOrDefaultAsync(urb => urb.Id == userRoomBookId, cancellationToken);
        if (userRoomBook == null) throw new UserRoomBookNotFoundException();

        var validationResult = await validator.ValidateExtendAsync(userRoomBook, request.ExtraDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Продлеваем срок
        userRoomBook.Deadline = userRoomBook.Deadline!.Value.AddDays(request.ExtraDays);
        userRoomBook.TotalExtendedDays += request.ExtraDays;

        await userRoomBookRepo.UpdateAsync(userRoomBook, cancellationToken);

        // SaveChanges и LogAsync выполняет база в одной транзакции.
        var audit = new AuditEntry(
            AuditActionType.Update,
            nameof(UserRoomBook),
            userRoomBook.Id.Value,
            $"Продлён срок возврата книги «{userRoomBook.RoomBook.Book.Title}» (+{request.ExtraDays} дн., новый срок: {userRoomBook.Deadline:d})");

        return (ResponseFactory.Success($"Срок продлен до {userRoomBook.Deadline:d}"), audit);
    }
}