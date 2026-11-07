namespace LibApp.Application.Commands.Entities.Users;

public class DeleteUserCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : IDeleteCommand<DeleteUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        {
            var userRepo = unitOfWork.GetRepository<User>();
            var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

            var user = await userRepo.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
            if (user == null) throw new UserNotFoundException();

            if (user.IsDeleted)
                return ResponseFactory.Success("Пользователь уже деактивирован");

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

            var deletedFullName = user.FullName;
            var deletedId = user.Id.Value;

            // Soft-delete: не удаляем физически, чтобы сохранить историю.
            // DeletedByUserId — инициатор удаления. null означает «legacy», трактуется
            // как «удалён админом». Self-delete (когда появится в UI) заполнит user.Id.
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedByUserId = currentUser.LibraryUserId;

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await userRepo.UpdateAsync(user, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await auditService.LogAsync(
                    AuditActionType.Delete,
                    nameof(User),
                    deletedId,
                    details: $"Деактивирован пользователь «{deletedFullName}»",
                    ct: cancellationToken);
            }, cancellationToken);

            return ResponseFactory.Success("Пользователь деактивирован");
        }
    }
}