namespace LibApp.Application.Commands.Entities.BorrowRequests;

/// <summary>
/// Создаёт заявку на выдачу от имени текущего пользователя.
/// Полная валидация выдачи (лимиты, просрочки, срок) выполняется при одобрении
/// в <c>IBorrowingService</c>: между созданием и одобрением состояние читателя может
/// измениться, дублирование проверки в двух местах — источник расхождений.
/// </summary>
public class CreateBorrowRequestCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<CreateBorrowRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        CreateBorrowRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUser.LibraryUserId
            ?? throw new UnauthorizedException("Не удалось определить текущего пользователя");

        var roomBookId = new Id(request.RoomBookId);

        var userRepo = UnitOfWork.GetRepository<User>();
        var roomBookRepo = UnitOfWork.GetRepository<RoomBook>();
        var borrowRequestRepo = UnitOfWork.GetRepository<BorrowRequest>();

        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UserNotFoundException();

        if (user.SubscriptionStatus != SubscriptionStatus.Active)
        {
            var statusName = user.SubscriptionStatus == SubscriptionStatus.Unconfirmed
                ? "не подтверждён"
                : "заморожен";
            throw new LibValidationException
            {
                ExceptionDetails = [$"Профиль пользователя {statusName}. Заявки на выдачу недоступны."]
            };
        }

        // Book и Room нужны для аудита (название книги, комната).
        // С LazyLoadingProxies навигации подтянутся при обращении;
        // явный include здесь не нужен, контекст scoped.
        var roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Id == roomBookId, cancellationToken)
            ?? throw new RoomBookNotFoundException("Экземпляр книги не найден в комнате");

        if (roomBook.AvailableCount <= 0)
            throw new LibValidationException
            {
                ExceptionDetails = ["Нет доступных экземпляров книги в этой комнате"]
            };

        // Pending-дубликат. Идемпотентности здесь нет: если читатель уже запросил эту книгу,
        // вторая заявка бессмысленна. Дублирующий guard на уровне БД — частичный
        // unique-индекс UX_BorrowRequest_Pending, он же защищает от race condition.
        if (await borrowRequestRepo.AnyAsync(
                br => br.UserId == userId
                    && br.RoomBookId == roomBookId
                    && br.Status == BorrowRequestStatus.Pending,
                cancellationToken))
        {
            throw new LibValidationException
            {
                ExceptionDetails = ["Заявка на эту книгу уже создана и ожидает рассмотрения"]
            };
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var borrowRequest = new BorrowRequest
        {
            Id = new Id(Guid.NewGuid()),
            UserId = userId,
            RoomBookId = roomBookId,
            Status = BorrowRequestStatus.Pending,
            CreatedAt = now
        };

        await borrowRequestRepo.AddAsync(borrowRequest, cancellationToken);

        var audit = new AuditEntry(
            AuditActionType.Create,
            nameof(BorrowRequest),
            borrowRequest.Id.Value,
            $"Создана заявка на выдачу книги «{roomBook.Book.Title}» (комната «{roomBook.Room.Name}») читателем «{user.FullName}»");

        return (ResponseFactory.Created<BorrowRequest>(), audit);
    }
}