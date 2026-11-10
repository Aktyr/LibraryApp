namespace LibApp.Application.Audit;

/// <summary>
/// Базовый класс write-команд: держит транзакцию, SaveChanges и LogAsync.
/// Наследники возвращают бизнес-результат и AuditEntry; база записывает аудит
/// в ту же транзакцию, что и изменение сущности.
/// </summary>
public abstract class AuditedCommandBase(
    IUnitOfWork unitOfWork,
    IAuditService auditService) : ICommand
{
    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    protected IAuditService AuditService { get; } = auditService;

    protected async Task<T> RunInTransactionAsync<T>(
        Func<Task<(T Response, AuditEntry? Audit)>> core,
        CancellationToken cancellationToken)
    {
        T response = default!;
        AuditEntry? audit = null;

        await UnitOfWork.ExecuteInTransactionAsync(async () =>
        {
            (response, audit) = await core();

            if (audit is not null)
            {
                await AuditService.LogAsync(
                    audit.Action,
                    audit.EntityType,
                    audit.EntityId,
                    audit.Details,
                    audit.UserId,
                    audit.UserName,
                    cancellationToken);
            }
        }, cancellationToken);

        return response;
    }
}