namespace LibApp.Application.Commands.Entities.BorrowRequests;

/// <summary>
/// Отклоняет заявку читателя. В отличие от одобрения не создаёт выдачу —
/// только меняет статус и фиксирует причину. Комментарий обязателен.
/// </summary>
public class RejectBorrowRequestCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<RejectBorrowRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        RejectBorrowRequest request, CancellationToken cancellationToken)
    {
        var processedBy = currentUser.LibraryUserId
            ?? throw new UnauthorizedException("Не удалось определить текущего пользователя");

        // Комментарий — обязательный атрибут отказа. Пробел/перевод строки не считаем причиной.
        var trimmedComment = request.Comment?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmedComment))
            throw new LibValidationException
            {
                ExceptionDetails = ["Необходимо указать причину отклонения заявки"]
            };

        if (trimmedComment.Length > BorrowRequestPolicy.MaxCommentLength)
            throw new LibValidationException
            {
                ExceptionDetails = [$"Комментарий не может превышать {BorrowRequestPolicy.MaxCommentLength} символов"]
            };

        var borrowRequestId = new Id(request.BorrowRequestId);
        var borrowRequestRepo = UnitOfWork.GetRepository<BorrowRequest>();

        // Book/Room подтягиваются через LazyLoadingProxies для текста аудита.
        var borrowRequest = await borrowRequestRepo.FirstOrDefaultAsync(
            br => br.Id == borrowRequestId, cancellationToken)
            ?? throw new LibValidationException { ExceptionDetails = ["Заявка не найдена"] };

        if (borrowRequest.Status != BorrowRequestStatus.Pending)
            throw new LibValidationException
            {
                ExceptionDetails = [$"Заявка уже обработана (текущий статус: {borrowRequest.Status})"]
            };

        borrowRequest.Status = BorrowRequestStatus.Rejected;
        borrowRequest.ProcessedAt = timeProvider.GetUtcNow().UtcDateTime;
        borrowRequest.ProcessedByUserId = processedBy;
        borrowRequest.Comment = trimmedComment;

        await borrowRequestRepo.UpdateAsync(borrowRequest, cancellationToken);

        // ФИО читателя, название книги и комментарий — в аудите, чтобы в отчёте было видно,
        // кому и почему отказали. Текст ошибки валидации в аудит не попадёт — там throw выше.
        var audit = new AuditEntry(
            AuditActionType.RejectBorrowRequest,
            nameof(BorrowRequest),
            borrowRequest.Id.Value,
            $"Отклонена заявка читателя «{borrowRequest.User.FullName}» на книгу «{borrowRequest.RoomBook.Book.Title}». Причина: {trimmedComment}");

        return (ResponseFactory.Success("Заявка отклонена"), audit);
    }
}