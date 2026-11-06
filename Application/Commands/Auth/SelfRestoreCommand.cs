namespace LibApp.Application.Commands.Auth;

/// <summary>
/// Самостоятельное восстановление аккаунта, удалённого самим пользователем
/// (<c>DeletedByUserId == user.Id</c>). Для удалённых администратором — отказ.
/// Пароль перепроверяется: токена у деактивированного нет, credentials — единственный фактор.
/// </summary>
public class SelfRestoreCommand(IUnitOfWork unitOfWork, IAuditService auditService)
    : ICreateOrUpdateCommand<SelfRestoreRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(SelfRestoreRequest request, CancellationToken ct)
    {
        var userRepo = unitOfWork.GetRepository<User>();

        // IgnoreQueryFilters: пользователь в статусе IsDeleted, глобальный фильтр его скрывает.
        var user = (await userRepo.ExecuteQueryIgnoringFiltersAsync(
            query => query.Where(u => u.Email == request.Email),
            ct)).FirstOrDefault();

        // Неверный email и неверный пароль дают одинаковое сообщение — защита от enumeration.
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Неверный email или пароль");

        if (!user.IsDeleted)
            throw new LibValidationException { ExceptionDetails = ["Аккаунт не деактивирован"] };

        // Self-restore — только для самостоятельно удалённых. Legacy (null) и
        // удалённые админом требуют вмешательства администратора.
        if (user.DeletedByUserId != user.Id)
            throw new LibValidationException
            {
                ExceptionDetails = ["Восстановление доступно только для самостоятельно удалённых аккаунтов"]
            };

        user.IsDeleted = false;
        user.DeletedAt = null;
        user.DeletedByUserId = null;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await userRepo.UpdateAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogAsync(
                AuditActionType.Restore,
                nameof(User),
                user.Id.Value,
                details: $"Самостоятельное восстановление аккаунта «{user.FullName}» ({user.Email})",
                userId: user.Id,
                userName: user.FullName,
                ct: ct);
        }, ct);

        return ResponseFactory.Success($"Аккаунт «{user.FullName}» восстановлен");
    }
}