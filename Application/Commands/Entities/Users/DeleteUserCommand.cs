namespace LibApp.Application.Commands.Entities.Users;

public class DeleteUserCommand(IUnitOfWork unitOfWork)
    : IDeleteCommand<DeleteUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var users = await userRepo.GetAsync(x => x.Id.Value == request.Id.Value, cancellationToken);
        var user = users.FirstOrDefault() ?? throw new UserNotFoundException();

        await userRepo.RemoveAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Deleted<User>();
    }
}