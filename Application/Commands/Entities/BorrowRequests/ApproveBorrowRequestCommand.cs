namespace LibApp.Application.Commands.Entities.BorrowRequests;

/// <summary>
/// Одобряет заявку читателя: активирует бронь на RoomBook.ReservedCount и устанавливает
/// BorrowRequest.ExpiresAt (now + BorrowingSettings.ReserveHoldHours). Фактическую выдачу
/// оформляет отдельная команда IssueBorrowRequestCommand. Полная валидация лимитов и просрочек
/// здесь не выполняется — читатель может получить отказ при попытке забрать книгу, если
/// за время брони ситуация ухудшилась. Предупреждения (лимит, просрочки) передаются
/// читателю в письме, но не блокируют Approve.
/// </summary>
public class ApproveBorrowRequestCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IOptionsSnapshot<BorrowingSettings> settings,
    INotificationService notificationService,
    ILogger<ApproveBorrowRequestCommand> logger,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<ApproveBorrowRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    // Заполняется в ExecuteCore, читается в Execute после коммита.
    // Команда transient, экземпляр живёт один вызов — состояние безопасно.
    private BorrowRequest? _approvedRequest;
    private List<string> _warnings = [];

    /// <summary>
    /// Выполняет транзакцию (бронь, смена статуса, аудит), после коммита отправляет email.
    /// Сбой SMTP логируется, но не пробрасывается: бронь уже активна, откат транзакции
    /// на основании ошибки почты невозможен.
    /// </summary>
    public override async Task<BasicCreateDeleteResponse> Execute(
        ApproveBorrowRequest request, CancellationToken cancellationToken)
    {
        var response = await base.Execute(request, cancellationToken);
        await TrySendApprovalEmailAsync();
        return response;
    }

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

        // Экземпляр должен быть в наличии: AvailableCount уже вычитает ReservedCount
        // других одобренных заявок. Если 0 — бронировать нечего, ошибка до транзакции.
        var roomBook = borrowRequest.RoomBook;
        if (roomBook.AvailableCount <= 0)
            throw new LibValidationException
            {
                ExceptionDetails = ["Нет доступных экземпляров для брони"]
            };

        var now = timeProvider.GetUtcNow().UtcDateTime;

        roomBook.ReservedCount++;

        borrowRequest.Status = BorrowRequestStatus.Approved;
        borrowRequest.ProcessedAt = now;
        borrowRequest.ProcessedByUserId = processedBy;
        borrowRequest.ExpiresAt = now.AddHours(settings.Value.ReserveHoldHours);
        borrowRequest.Comment = null;

        await borrowRequestRepo.UpdateAsync(borrowRequest, cancellationToken);

        // Предупреждения для письма. Approve не блокируется, но читатель должен знать,
        // что бронь не даёт права забрать книгу, пока не исправлена ситуация.
        _warnings = CollectWarnings(borrowRequest.User, now, settings.Value);

        var audit = new AuditEntry(
            AuditActionType.ApproveBorrowRequest,
            nameof(BorrowRequest),
            borrowRequest.Id.Value,
            $"Одобрена заявка читателя «{borrowRequest.User.FullName}» на книгу «{roomBook.Book.Title}». Бронь до: {borrowRequest.ExpiresAt:dd.MM.yyyy HH:mm}");

        _approvedRequest = borrowRequest;

        return (ResponseFactory.Success($"Заявка одобрена. Бронь до: {borrowRequest.ExpiresAt:dd.MM.yyyy HH:mm}"), audit);
    }

    private static List<string> CollectWarnings(User user, DateTime now, BorrowingSettings s)
    {
        var warnings = new List<string>();

        var activeCount = user.RoomBooks.Count(urb => !urb.IsReturned);
        if (activeCount >= s.MaxBooksPerUser)
            warnings.Add($"У вас на руках {activeCount} из {s.MaxBooksPerUser} книг. " +
                         $"Верните хотя бы одну, чтобы забрать бронь.");

        var hasOverdue = user.RoomBooks.Any(urb =>
            !urb.IsReturned && urb.Deadline.HasValue && urb.Deadline.Value < now);
        if (hasOverdue)
            warnings.Add("У вас есть просроченные книги. Верните их, чтобы забрать бронь.");

        return warnings;
    }

    private async Task TrySendApprovalEmailAsync()
    {
        if (_approvedRequest is null) return;

        try
        {
            // CancellationToken.None: транзакция закоммичена, клиент мог отвалиться.
            // Письмо нужно отправить независимо от состояния HTTP-запроса.
            await notificationService.SendBorrowRequestApprovedAsync(
                _approvedRequest.User,
                _approvedRequest,
                _warnings,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Borrow request {BorrowRequestId} approved, but approval email failed",
                _approvedRequest.Id.Value);
        }
    }
}