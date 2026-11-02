namespace LibApp.Application.Commands.Entities.Users;

/// <summary>
/// Изменяет статус абонемента пользователя. Пишет запись в аудит в той же транзакции.
/// </summary>
public class ChangeSubscriptionStatusCommand(
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : ICreateOrUpdateCommand<ChangeSubscriptionStatusRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(ChangeSubscriptionStatusRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null) throw new UserNotFoundException();

        var oldStatus = user.SubscriptionStatus;
        user.SubscriptionStatus = request.NewStatus;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await userRepo.UpdateAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.LogAsync(
                AuditActionType.Update,
                nameof(User),
                user.Id.Value,
                details: $"Изменён статус профиля «{user.FullName}»: {oldStatus} → {request.NewStatus}",
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Success($"Статус изменён на {request.NewStatus}");
    }
}