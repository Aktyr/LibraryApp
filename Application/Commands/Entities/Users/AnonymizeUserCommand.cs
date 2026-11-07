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
                ExceptionDetails = ["Нельзя анонимизировать администратора"]
            };

        if (user.IsDeleted)
            throw new LibValidationException
            {
                ExceptionDetails = ["Пользователь уже деактивирован. Анонимизация доступна только для активных."]
            };

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

        // Неоплаченный штраф = есть начисление, но нет ни одного платежа.
        // After ReturnDate-only penalty: штраф может быть начислен только после возврата,
        // так что ReturnDate != null подразумевается, но явная проверка безопаснее.
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
                    $"Нельзя анонимизировать пользователя: {unpaidPenalties} неоплаченных штрафов. Сначала примите оплату или спишите."
                ]
            };

        var deletedFullName = user.FullName;
        var deletedId = user.Id.Value;
        var anonymizedEmail = $"deleted_{deletedId:N}@library.local";

        user.LastName = "Deleted";
        user.FirstName = "User";
        user.MiddleName = string.Empty;
        user.Email = anonymizedEmail;
        user.ContactInfo = string.Empty;
        user.PasswordHash = string.Empty;
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
                details: $"Анонимизирован пользователь (ранее: «{deletedFullName}»). Email: {anonymizedEmail}",
                userId: currentUser.LibraryUserId,
                userName: currentUser.FullName,
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Success("Пользователь анонимизирован");
    }
}