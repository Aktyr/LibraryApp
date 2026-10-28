namespace LibApp.Application.Commands.Entities.Users;

public class DeleteUserCommand(IUnitOfWork unitOfWork)
    : IDeleteCommand<DeleteUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user == null) throw new UserNotFoundException();

        var activeBorrows = await userRoomBookRepo.CountAsync(
            urb => urb.User.Id == request.Id && urb.ReturnDate == null,
            cancellationToken);

        if (activeBorrows > 0)
        {
            throw new LibValidationException
            {
                ExceptionDetails =
                [
                    $"Нельзя удалить пользователя: у него {activeBorrows} активных выдач. " +
                    "Сначала верните все книги."
                ]
            };
        }

        await userRepo.RemoveAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Deleted<User>();
    }
}