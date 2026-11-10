namespace LibApp.Application.Commands.Entities.Users;

public class RestoreUserCommand(IUnitOfWork unitOfWork, IAuditService auditService)
    : AuditedCreateOrUpdateCommand<RestoreUserRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        RestoreUserRequest request, CancellationToken ct)
    {
        var repo = UnitOfWork.GetRepository<User>();

        var user = (await repo.ExecuteQueryIgnoringFiltersAsync(
            query => query.Where(u => u.Id == request.Id),
            ct)).FirstOrDefault();

        if (user == null) throw new UserNotFoundException();

        // Ранний выход без аудита: пользователь уже активен.
        if (!user.IsDeleted)
            return (ResponseFactory.Success("Пользователь уже активен"), null);

        user.IsDeleted = false;
        user.DeletedAt = null;

        await repo.UpdateAsync(user, ct);

        var audit = new AuditEntry(
            AuditActionType.Restore,
            nameof(User),
            user.Id.Value,
            $"Восстановлен пользователь «{user.FullName}»");

        return (ResponseFactory.Success($"Пользователь «{user.FullName}» восстановлен"), audit);
    }
}