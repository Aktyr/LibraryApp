namespace LibApp.Application.Commands.Entities.Users;

/// <summary>
/// Самостоятельное удаление аккаунта пользователем из личного кабинета.
/// Admin не может удалить себя: защита от «удалил последнего админа».
/// Identity-запись удаляется в BFF до вызова команды.
/// </summary>
public class SelfDeleteCommand(
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : AuditedDeleteCommand<DeleteUserRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        DeleteUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = UnitOfWork.GetRepository<User>();
        var userRoomBookRepo = UnitOfWork.GetRepository<UserRoomBook>();

        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user == null) throw new UserNotFoundException();

        if (user.Role == UserRole.Admin)
            throw new LibValidationException
            {
                ExceptionDetails = ["Администратор не может удалить свой аккаунт. Обратитесь к другому администратору."]
            };

        var activeBorrows = await userRoomBookRepo.CountAsync(
            urb => urb.User.Id == request.Id && urb.ReturnDate == null,
            cancellationToken);

        if (activeBorrows > 0)
            throw new LibValidationException
            {
                ExceptionDetails =
                [
                    $"Нельзя удалить аккаунт: у вас {activeBorrows} активных выдач. Сначала верните все книги."
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
                    $"Нельзя удалить аккаунт: у вас {unpaidPenalties} неоплаченных штрафов. " +
                    "Обратитесь к библиотекарю."
                ]
            };

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        // Self-delete: инициатор = сам пользователь, чтобы self-restore сработал.
        user.DeletedByUserId = user.Id;

        await userRepo.UpdateAsync(user, cancellationToken);

        // userId/userName передаём явно: пользователь удаляется, ICurrentUserService
        // уже не резолвится корректно в следующих запросах. Снимок для аудита нужен здесь.
        var audit = new AuditEntry(
            AuditActionType.Delete,
            nameof(User),
            user.Id.Value,
            $"Самостоятельное удаление аккаунта «{user.FullName}»",
            UserId: user.Id,
            UserName: user.FullName);

        return (ResponseFactory.Success("Аккаунт деактивирован. Восстановить можно по email и паролю."), audit);
    }
}