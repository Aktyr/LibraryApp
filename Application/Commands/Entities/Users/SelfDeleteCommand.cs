namespace LibApp.Application.Commands.Entities.Users;

/// <summary>
/// Самостоятельное удаление аккаунта пользователем из личного кабинета.
/// Admin не может удалить себя: защита от «удалил последнего админа».
/// Identity-запись удаляется в BFF до вызова команды.
/// </summary>
public class SelfDeleteCommand(
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : IDeleteCommand<DeleteUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

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

        var deletedFullName = user.FullName;
        var deletedId = user.Id.Value;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        // Self-delete: инициатор = сам пользователь, чтобы self-restore сработал.
        user.DeletedByUserId = user.Id;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await userRepo.UpdateAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.LogAsync(
                AuditActionType.Delete,
                nameof(User),
                deletedId,
                details: $"Самостоятельное удаление аккаунта «{deletedFullName}»",
                userId: user.Id,
                userName: deletedFullName,
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Success("Аккаунт деактивирован. Восстановить можно по email и паролю.");
    }
}