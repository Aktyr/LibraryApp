namespace LibApp.Application.Commands.Entities.Users;

public class DeleteUserCommand(IUnitOfWork unitOfWork)
    : IDeleteCommand<DeleteUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var users = await userRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var user = users.FirstOrDefault() ?? throw new UserNotFoundException();

        await userRepo.Remove(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Deleted<User>();
    }
}