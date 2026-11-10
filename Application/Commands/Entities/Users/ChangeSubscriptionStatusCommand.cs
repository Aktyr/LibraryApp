namespace LibApp.Application.Commands.Entities.Users;

/// <summary>
/// Изменяет статус абонемента пользователя. Пишет запись в аудит в той же транзакции.
/// </summary>
public class ChangeSubscriptionStatusCommand(
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<ChangeSubscriptionStatusRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        ChangeSubscriptionStatusRequest request, CancellationToken cancellationToken)
    {
        var userRepo = UnitOfWork.GetRepository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null) throw new UserNotFoundException();

        var oldStatus = user.SubscriptionStatus;
        user.SubscriptionStatus = request.NewStatus;

        await userRepo.UpdateAsync(user, cancellationToken);

        var audit = new AuditEntry(
            AuditActionType.Update,
            nameof(User),
            user.Id.Value,
            $"Изменён статус профиля «{user.FullName}»: {oldStatus} → {request.NewStatus}");

        return (ResponseFactory.Success($"Статус изменён на {request.NewStatus}"), audit);
    }
}