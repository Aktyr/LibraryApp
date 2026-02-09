namespace LibApp.Application.Entities.Users;

public class DeleteUserCommand(IRepository<User> userRepo)
    : IDeleteCommand<DeleteUserRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        var users = await userRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var user = users.FirstOrDefault() ?? throw new UserNotFoundException();

        await userRepo.Remove(user, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "User deleted successfully.");
    }
}