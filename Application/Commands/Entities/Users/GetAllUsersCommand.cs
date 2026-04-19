namespace LibApp.Application.Commands.Entities.Users;

public class GetAllUsersQuery(IRepository<User> userRepo, IConverter<User, UserDTO> userConverter)
    : IGetQuery<EmptyRequest, UserResponse>, ICommand
{
    public async Task<UserResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        var users = await userRepo.GetWithoutTracking(cancellationToken);
        var userDTOs = users.Select(user => userConverter.ToDto(user)).ToArray();

        return new UserResponse("Ok", "List of users issued successfully.", userDTOs);
    }
}