namespace LibApp.Application.Commands.Entities.Users;

/// <summary>
/// Анонимизация пользователя: персональные данные (ФИО, email, контакт, пароль)
/// необратимо затираются, пользователь помечается удалённым. История выдач,
/// штрафов и аудита сохраняется для отчётности.
/// Identity-запись удаляется в BFF до вызова команды.
/// </summary>
public class AnonymizeUserCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : AuditedDeleteCommand<DeleteUserRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        DeleteUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = UnitOfWork.GetRepository<User>();
        var userRoomBookRepo = UnitOfWork.GetRepository<UserRoomBook>();

        // Обход глобального HasQueryFilter(IsDeleted): анонимизация доступна только
        // для удалённых пользователей, обычный FirstOrDefaultAsync вернёт null.
        var user = (await userRepo.ExecuteQueryIgnoringFiltersAsync(
            q => q.Where(u => u.Id == request.Id),
            cancellationToken)).FirstOrDefault();

        if (user == null) throw new UserNotFoundException();

        if (user.Role == UserRole.Admin)
            throw new LibValidationException
            {
                ExceptionDetails = ["Нельзя анонимизировать администратора"]
            };

        if (!user.IsDeleted)
            throw new LibValidationException
            {
                ExceptionDetails =
                [
                    "Анонимизация доступна только для удалённых пользователей. " +
                    "Сначала деактивируйте пользователя."
                ]
            };

        if (user.Email.StartsWith("deleted_", StringComparison.Ordinal))
            throw new LibValidationException
            {
                ExceptionDetails = ["Пользователь уже анонимизирован"]
            };

        // Удалённый пользователь мог быть удалён до введения этих проверок,
        // либо удаление было через прямой SQL. Перепроверяем перед затиранием данных.
        var activeBorrows = await userRoomBookRepo.CountAsync(
            urb => urb.User.Id == request.Id && urb.ReturnDate == null,
            cancellationToken);

        if (activeBorrows > 0)
            throw new LibValidationException
            {
                ExceptionDetails =
                [
                    $"Нельзя анонимизировать пользователя: {activeBorrows} активных выдач. Сначала верните все книги."
                ]
            };

        var unpaidPenalties = await userRoomBookRepo.CountAsync(
            urb => urb.User.Id == request.Id
                   && urb.Penalty.HasValue && urb.Penalty > 0
                   && !urb.PenaltyPayments.Any(),
            cancellationToken);

        if (unpaidPenalties > 0)
            throw new LibValidationException
            {
                ExceptionDetails =
                [
                    $"Нельзя анонимизировать пользователя: {unpaidPenalties} неоплаченных штрафов. " +
                    "Сначала примите оплату или спишите."
                ]
            };

        var userId = user.Id.Value;
        var anonymizedEmail = $"deleted_{userId:N}@library.local";

        user.LastName = "Deleted";
        user.FirstName = "User";
        user.MiddleName = string.Empty;
        user.Email = anonymizedEmail;
        user.ContactInfo = string.Empty;
        user.PasswordHash = string.Empty;

        await userRepo.UpdateAsync(user, cancellationToken);

        // PII (ФИО, старый email) не пишем в аудит: цель GDPR-анонимизации — стереть данные.
        // userId достаточно для аудиторской связи с оригиналом через историю выдач.
        var actorName = currentUser.FullName ?? "администратор";
        var audit = new AuditEntry(
            AuditActionType.Anonymize,
            nameof(User),
            userId,
            $"library: персональные данные затёрты (ID {userId:N}). " +
            $"Identity: удаление выполняется BFF. " +
            $"Администратор: «{actorName}». Новый email: {anonymizedEmail}",
            UserId: currentUser.LibraryUserId,
            UserName: currentUser.FullName);

        return (ResponseFactory.Success("Пользователь анонимизирован"), audit);
    }
}