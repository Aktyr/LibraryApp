namespace LibApp.Application.Audit;

public abstract class AuditedCreateOrUpdateCommand<TReq, TResp>(IUnitOfWork unitOfWork, IAuditService auditService)
    : AuditedCommandBase(unitOfWork, auditService), ICreateOrUpdateCommand<TReq, TResp>
    where TReq : IAddOrUpdateRequest
    where TResp : IAddOrUpdateResponse
{
    // virtual — чтобы команды могли выполнить побочный эффект после коммита
    // (например, отправить email), не растягивая транзакцию.
    public virtual Task<TResp> Execute(TReq request, CancellationToken cancellationToken)
        => RunInTransactionAsync(() => ExecuteCore(request, cancellationToken), cancellationToken);

    protected abstract Task<(TResp Response, AuditEntry? Audit)> ExecuteCore(
        TReq request, CancellationToken cancellationToken);
}