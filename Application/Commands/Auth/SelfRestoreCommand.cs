namespace LibApp.Application.Commands.Auth;

/// <summary>
/// Самостоятельное восстановление аккаунта, удалённого самим пользователем
/// (<c>DeletedByUserId == user.Id</c>). Для удалённых администратором — отказ.
/// Пароль перепроверяется: токена у деактивированного нет, credentials — единственный фактор.
/// </summary>
public class SelfRestoreCommand(IUnitOfWork unitOfWork, IAuditService auditService)
    : AuditedCreateOrUpdateCommand<SelfRestoreRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        SelfRestoreRequest request, CancellationToken ct)
    {
        var userRepo = UnitOfWork.GetRepository<User>();

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

        await userRepo.UpdateAsync(user, ct);

        // ICurrentUserService пуст: пользователь только что восстановлен, Identity-сессии нет.
        // UserId/UserName передаём явно из восстановленной записи — иначе AuditService
        // подставит «Неизвестный пользователь».
        var audit = new AuditEntry(
            AuditActionType.Restore,
            nameof(User),
            user.Id.Value,
            $"Самостоятельное восстановление аккаунта «{user.FullName}» ({user.Email})",
            UserId: user.Id,
            UserName: user.FullName);

        return (ResponseFactory.Success($"Аккаунт «{user.FullName}» восстановлен"), audit);
    }
}