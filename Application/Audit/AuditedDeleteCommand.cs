namespace LibApp.Application.Audit;

public abstract class AuditedDeleteCommand<TReq, TResp>(IUnitOfWork unitOfWork, IAuditService auditService)
    : AuditedCommandBase(unitOfWork, auditService), IDeleteCommand<TReq, TResp>
    where TReq : IDeleteRequest
    where TResp : IDeleteResponse
{
    public Task<TResp> Execute(TReq request, CancellationToken cancellationToken)
        => RunInTransactionAsync(() => ExecuteCore(request, cancellationToken), cancellationToken);

    protected abstract Task<(TResp Response, AuditEntry? Audit)> ExecuteCore(
        TReq request, CancellationToken cancellationToken);
}