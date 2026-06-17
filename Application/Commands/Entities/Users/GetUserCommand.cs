namespace LibApp.Application.Commands.Entities.Users;

public class GetUserQuery(IRepository<User> userRepo, IConverter<User, UserDTO> userConverter) : IGetQuery<GetUserRequest, UserResponse>, ICommand
{
    public async Task<UserResponse?> Execute(GetUserRequest request, CancellationToken cancellationToken)
    {
        var users = await userRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var user = users.FirstOrDefault() ?? throw new UserNotFoundException();

        var userDto = userConverter.ToDto(user);

        return ResponseFactory.Single<User, UserDTO, UserResponse>(userDto);
    }
}