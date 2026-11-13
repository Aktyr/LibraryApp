namespace LibApp.Application.Commands.Entities.BorrowRequests;

/// <summary>
/// Одобряет заявку читателя: создаёт фактическую выдачу через <c>IBorrowingService</c>
/// (та же проверка лимитов и просрочек, что в прямой выдаче библиотекарем) и переводит
/// заявку в Approved. Пишет два аудита — ApproveBorrowRequest (базовый механизм) и Borrow
/// (явно, чтобы не терять историю выдач, оформленных через заявки).
/// </summary>
public class ApproveBorrowRequestCommand(
    IUnitOfWork unitOfWork,
    IBorrowingService borrowingService,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<ApproveBorrowRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        ApproveBorrowRequest request, CancellationToken cancellationToken)
    {
        var processedBy = currentUser.LibraryUserId
            ?? throw new UnauthorizedException("Не удалось определить текущего пользователя");

        var borrowRequestId = new Id(request.BorrowRequestId);
        var borrowRequestRepo = UnitOfWork.GetRepository<BorrowRequest>();

        var borrowRequest = await borrowRequestRepo.FirstOrDefaultAsync(
            br => br.Id == borrowRequestId, cancellationToken)
            ?? throw new LibValidationException { ExceptionDetails = ["Заявка не найдена"] };

        if (borrowRequest.Status != BorrowRequestStatus.Pending)
            throw new LibValidationException
            {
                ExceptionDetails = [$"Заявка уже обработана (текущий статус: {borrowRequest.Status})"]
            };

        // IBorrowingService при провале валидации бросит LibValidationException —
        // заявка останется Pending, библиотекарь увидит причину и сможет отклонить.
        var userRoomBook = await borrowingService.BorrowAsync(
            userId: borrowRequest.UserId,
            roomBookId: borrowRequest.RoomBookId,
            borrowDays: request.BorrowDays,
            borrowRequestId: borrowRequest.Id,
            cancellationToken);

        borrowRequest.Status = BorrowRequestStatus.Approved;
        borrowRequest.ProcessedAt = timeProvider.GetUtcNow().UtcDateTime;
        borrowRequest.ProcessedByUserId = processedBy;
        borrowRequest.Comment = null;

        await borrowRequestRepo.UpdateAsync(borrowRequest, cancellationToken);

        await AuditService.LogAsync(
            AuditActionType.Borrow,
            nameof(UserRoomBook),
            userRoomBook.Id.Value,
            details: $"Выдана книга «{userRoomBook.RoomBook.Book.Title}» пользователю «{userRoomBook.User.FullName}» по заявке. Срок: {userRoomBook.Deadline:d}",
            ct: cancellationToken);

        var audit = new AuditEntry(
            AuditActionType.ApproveBorrowRequest,
            nameof(BorrowRequest),
            borrowRequest.Id.Value,
            $"Одобрена заявка читателя «{userRoomBook.User.FullName}» на книгу «{userRoomBook.RoomBook.Book.Title}». Срок возврата: {userRoomBook.Deadline:d}");

        return (ResponseFactory.Success($"Заявка одобрена. Срок возврата: {userRoomBook.Deadline:d}"), audit);
    }
}