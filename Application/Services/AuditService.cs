namespace LibApp.Application.Services;

/// <summary>Сервис аудита. Пишет записи в БД в рамках текущего UnitOfWork.</summary>
public class AuditService(IUnitOfWork unitOfWork, ICurrentUserService currentUser) : IAuditService, IService
{
    public async Task LogAsync(
        AuditActionType action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        Id? userId = null,
        string? userName = null,
        CancellationToken ct = default)
    {
        // При логине ICurrentUserService пуст, поэтому ID и ФИО передаются явно.
        var finalUserId = userId ?? currentUser.LibraryUserId;
        var finalUserName = userName ?? currentUser.FullName;

        if (string.IsNullOrWhiteSpace(finalUserName))
            finalUserName = "Неизвестный пользователь";

        var log = new AuditLog
        {
            Id = new Id(Guid.NewGuid()),
            Timestamp = DateTime.UtcNow,
            UserId = finalUserId,
            UserName = AuditPolicy.Truncate(finalUserName, AuditPolicy.UserNameMaxLength)!,
            ActionType = action,
            EntityType = AuditPolicy.Truncate(entityType, AuditPolicy.EntityTypeMaxLength)!,
            EntityId = entityId,
            Details = AuditPolicy.Truncate(details, AuditPolicy.DetailsMaxLength)
        };

        var repo = unitOfWork.GetRepository<AuditLog>();
        await repo.AddAsync(log, ct);

        // ВАЖНО: Мы не вызываем SaveChangesAsync, чтобы позволить основной команде
        // завершить транзакцию. Запись аудита зафиксируется вместе с ней.
    }
}