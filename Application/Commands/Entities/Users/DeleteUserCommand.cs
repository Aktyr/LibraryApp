namespace LibApp.Application.Commands.Entities.Users;

public class DeleteUserCommand(
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

        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user == null) throw new UserNotFoundException();

        // Ранний выход без аудита: база откроет транзакцию, но LogAsync не вызовется.
        if (user.IsDeleted)
            return (ResponseFactory.Success("Пользователь уже деактивирован"), null);

        var activeBorrows = await userRoomBookRepo.CountAsync(
            urb => urb.User.Id == request.Id && urb.ReturnDate == null,
            cancellationToken);

        if (activeBorrows > 0)
        {
            throw new LibValidationException
            {
                ExceptionDetails =
                [
                    $"Нельзя деактивировать пользователя: у него {activeBorrows} активных выдач. " +
                    "Сначала верните все книги."
                ]
            };
        }

        var unpaidPenalties = await userRoomBookRepo.CountAsync(
            urb => urb.User.Id == request.Id
                   && urb.Penalty.HasValue && urb.Penalty > 0
                   && !urb.PenaltyPayments.Any(),
            cancellationToken);

        if (unpaidPenalties > 0)
        {
            throw new LibValidationException
            {
                ExceptionDetails =
                [
                    $"Нельзя деактивировать пользователя: у него {unpaidPenalties} неоплаченных штрафов. " +
                    "Сначала примите оплату или спишите штраф."
                ]
            };
        }

        // Soft-delete: не удаляем физически, чтобы сохранить историю.
        // DeletedByUserId — инициатор удаления. null означает «legacy», трактуется
        // как «удалён админом». Self-delete заполняет user.Id.
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedByUserId = currentUser.LibraryUserId;

        await userRepo.UpdateAsync(user, cancellationToken);

        var audit = new AuditEntry(
            AuditActionType.Delete,
            nameof(User),
            user.Id.Value,
            $"Деактивирован пользователь «{user.FullName}»");

        return (ResponseFactory.Success("Пользователь деактивирован"), audit);
    }
}