namespace LibApp.Application.Commands.Entities.Users;

public class RestoreUserCommand(IUnitOfWork unitOfWork, IAuditService auditService)
    : ICreateOrUpdateCommand<RestoreUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(RestoreUserRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<User>();

        var user = (await repo.ExecuteQueryIgnoringFiltersAsync(
            query => query.Where(u => u.Id == request.Id),
            ct)).FirstOrDefault();

        if (user == null) throw new UserNotFoundException();
        if (!user.IsDeleted) return ResponseFactory.Success("Пользователь уже активен");

        user.IsDeleted = false;
        user.DeletedAt = null;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await repo.UpdateAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogAsync(
                AuditActionType.Restore,
                nameof(User),
                user.Id.Value,
                details: $"Восстановлен пользователь «{user.FullName}»",
                ct: ct);
        }, ct);

        return ResponseFactory.Success($"Пользователь «{user.FullName}» восстановлен");
    }
}