namespace LibApp.Application.Commands.Entities.BorrowRequests;

/// <summary>
/// Оформляет фактическую выдачу по брони (BorrowRequest.Status = Approved). Создаёт
/// UserRoomBook через IBorrowingService (та же валидация лимитов и просрочек, что
/// в прямой выдаче), декрементит RoomBook.ReservedCount, переводит заявку в Completed,
/// заполняет PickedUpAt. Email-подтверждение «книга выдана» отправляется после коммита:
/// сбой SMTP не откатывает выдачу.
/// </summary>
public class IssueBorrowRequestCommand(
    IUnitOfWork unitOfWork,
    IBorrowingService borrowingService,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    INotificationService notificationService,
    ILogger<IssueBorrowRequestCommand> logger,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<IssueBorrowRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    // Заполняется в ExecuteCore, читается в Execute после коммита.
    // Команда transient, экземпляр живёт один вызов — состояние безопасно.
    private UserRoomBook? _issuedBorrow;

    /// <summary>
    /// Выполняет транзакцию (создание выдачи, декремент брони, смена статуса, аудит),
    /// после коммита отправляет email. Ошибка SMTP логируется, но не пробрасывается.
    /// </summary>
    public override async Task<BasicCreateDeleteResponse> Execute(
        IssueBorrowRequest request, CancellationToken cancellationToken)
    {
        var response = await base.Execute(request, cancellationToken);
        await TrySendConfirmationEmailAsync();
        return response;
    }

    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        IssueBorrowRequest request, CancellationToken cancellationToken)
    {
        // processedBy не записывается в BorrowRequest: ProcessedByUserId фиксирует
        // решение по заявке (Approve/Reject), а Issue — отдельная операция, её автор
        // останется в AuditLog через ICurrentUserService базового LogAsync.
        _ = currentUser.LibraryUserId
            ?? throw new UnauthorizedException("Не удалось определить текущего пользователя");

        var borrowRequestId = new Id(request.BorrowRequestId);
        var borrowRequestRepo = UnitOfWork.GetRepository<BorrowRequest>();

        var borrowRequest = await borrowRequestRepo.FirstOrDefaultAsync(
            br => br.Id == borrowRequestId, cancellationToken)
            ?? throw new LibValidationException { ExceptionDetails = ["Заявка не найдена"] };

        if (borrowRequest.Status != BorrowRequestStatus.Approved)
            throw new LibValidationException
            {
                ExceptionDetails = [$"Выдача возможна только по одобренной заявке (текущий статус: {borrowRequest.Status})"]
            };

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Фоновый ExpireBorrowRequestsService переводит просроченные брони в Expired,
        // но между его циклами возможна выдача по мёртвой брони. Проверка здесь —
        // последняя линия: выдавать по истёкшей брони нельзя, читатель должен
        // оформить новую заявку.
        if (borrowRequest.ExpiresAt is { } expiresAt && now > expiresAt)
            throw new LibValidationException
            {
                ExceptionDetails = [$"Бронь истекла {expiresAt:dd.MM.yyyy HH:mm}. Читатель должен оформить новую заявку."]
            };

        // BorrowAsync делает полную валидацию: профиль, лимит книг на руках,
        // просрочки, срок, дубликат выдачи этой же книги. Если что-то не так —
        // LibValidationException, бронь остаётся Approved, библиотекарь видит причину.
        var userRoomBook = await borrowingService.BorrowAsync(
            userId: borrowRequest.UserId,
            roomBookId: borrowRequest.RoomBookId,
            borrowDays: request.BorrowDays,
            borrowRequestId: borrowRequest.Id,
            cancellationToken);

        // Бронь превратилась в выдачу: зарезервированный экземпляр теперь Borrowed.
        // AvailableCount не меняется (BorrowedCount +1, ReservedCount -1), но
        // инвариант BookCount = Borrowed + Reserved + Available сохранён.
        borrowRequest.RoomBook.ReservedCount--;

        borrowRequest.Status = BorrowRequestStatus.Completed;
        borrowRequest.PickedUpAt = now;

        await borrowRequestRepo.UpdateAsync(borrowRequest, cancellationToken);

        // Один аудит — Borrow, как в прямой выдаче через BorrowBookCommand.
        // ApproveBorrowRequest уже записан при Approve, второй раз не дублируем.
        var audit = new AuditEntry(
            AuditActionType.Borrow,
            nameof(UserRoomBook),
            userRoomBook.Id.Value,
            $"Выдана книга «{userRoomBook.RoomBook.Book.Title}» пользователю «{userRoomBook.User.FullName}» по брони. Срок: {userRoomBook.Deadline:d}");

        _issuedBorrow = userRoomBook;

        return (ResponseFactory.Success($"Книга выдана. Срок возврата: {userRoomBook.Deadline:d}"), audit);
    }

    private async Task TrySendConfirmationEmailAsync()
    {
        if (_issuedBorrow is null) return;

        try
        {
            // CancellationToken.None: транзакция закоммичена, клиент мог отвалиться.
            await notificationService.SendBorrowConfirmationAsync(
                _issuedBorrow.User,
                _issuedBorrow,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Borrow request {BorrowRequestId} issued, but confirmation email failed",
                _issuedBorrow.BorrowRequestId?.Value);
        }
    }
}