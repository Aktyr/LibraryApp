namespace LibApp.Application.Commands.Entities.BorrowRequests;

/// <summary>
/// Отменяет заявку читателем. Разрешено только владельцу заявки: UserId берётся
/// из <c>ICurrentUserService</c>, а не из запроса — подмена невозможна.
/// Для Approved-брони декрементирует <c>RoomBook.ReservedCount</c>: экземпляр
/// возвращается в оборот для других читателей. Email не отправляется: читатель сам
/// инициировал действие, отдельное уведомление избыточно.
/// </summary>
public class CancelBorrowRequestCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<CancelBorrowRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        CancelBorrowRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUser.LibraryUserId
            ?? throw new UnauthorizedException("Не удалось определить текущего пользователя");

        // Комментарий опционален, но если передан — проверяем длину до записи в БД.
        // null и пустая строка трактуются одинаково: причина не указана.
        var trimmedComment = string.IsNullOrWhiteSpace(request.Comment)
            ? null
            : request.Comment.Trim();

        if (trimmedComment is { Length: > BorrowRequestPolicy.MaxCommentLength })
            throw new LibValidationException
            {
                ExceptionDetails = [$"Комментарий не может превышать {BorrowRequestPolicy.MaxCommentLength} символов"]
            };

        var borrowRequestId = new Id(request.BorrowRequestId);
        var borrowRequestRepo = UnitOfWork.GetRepository<BorrowRequest>();

        var borrowRequest = await borrowRequestRepo.FirstOrDefaultAsync(
            br => br.Id == borrowRequestId, cancellationToken)
            ?? throw new LibValidationException { ExceptionDetails = ["Заявка не найдена"] };

        // Только свои заявки. Библиотекарь отменяет через Reject (Pending) — отдельной
        // команды «отменить чужую бронь» нет, это не бизнес-сценарий: бронь закрывается
        // через Expire (не пришёл), через Issue (забрал) или через Cancel самим читателем.
        if (borrowRequest.UserId != userId)
            throw new LibValidationException
            {
                ExceptionDetails = ["Отменить можно только свою заявку"]
            };

        // Отменять можно Pending (ещё не решено) и Approved (бронь активна).
        // Rejected/Completed/Cancelled/Expired — терминальные, отмена не применима.
        if (borrowRequest.Status is not (BorrowRequestStatus.Pending or BorrowRequestStatus.Approved))
            throw new LibValidationException
            {
                ExceptionDetails = [$"Нельзя отменить заявку в статусе {borrowRequest.Status}"]
            };

        // Для Approved-брони экземпляр был зарезервирован при Approve — возвращаем в оборот.
        // Для Pending ReservedCount не трогали, инкремента не было.
        if (borrowRequest.Status == BorrowRequestStatus.Approved)
        {
            borrowRequest.RoomBook.ReservedCount--;
        }

        var previousStatus = borrowRequest.Status;
        borrowRequest.Status = BorrowRequestStatus.Cancelled;
        borrowRequest.ProcessedAt = timeProvider.GetUtcNow().UtcDateTime;
        // ProcessedByUserId не заполняется: обработчика нет, это действие самого читателя.
        // Атрибут «кто обработал» — для Approve/Reject (библиотекарь). Для Cancel
        // автор остаётся в AuditLog через ICurrentUserService.
        if (trimmedComment is not null)
            borrowRequest.Comment = trimmedComment;

        await borrowRequestRepo.UpdateAsync(borrowRequest, cancellationToken);

        var commentSuffix = trimmedComment is null ? "" : $". Причина: {trimmedComment}";
        var audit = new AuditEntry(
            AuditActionType.Update,
            nameof(BorrowRequest),
            borrowRequest.Id.Value,
            $"Отменена заявка на книгу «{borrowRequest.RoomBook.Book.Title}» (была {previousStatus}){commentSuffix}");

        return (ResponseFactory.Success("Заявка отменена"), audit);
    }
}