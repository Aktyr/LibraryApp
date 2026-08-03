namespace LibApp.Application.Commands.Entities.Users;

public class DeleteUserCommand(IUnitOfWork unitOfWork)
    : IDeleteCommand<DeleteUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Id.Value == request.Id.Value, cancellationToken);
        if (user == null) throw new UserNotFoundException();

        await userRepo.RemoveAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Deleted<User>();
    }
}