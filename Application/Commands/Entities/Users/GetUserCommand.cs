namespace LibApp.Application.Commands.Entities.Users;

public class GetUserCommand(IUnitOfWork unitOfWork, IConverter<User, UserDTO> userConverter) : IGetQuery<GetUserRequest, UserResponse>, ICommand
{
    public async Task<UserResponse?> Execute(GetUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user == null) throw new UserNotFoundException();

        var userDto = userConverter.ToDto(user);

        return ResponseFactory.Single<User, UserDTO, UserResponse>(userDto);
    }
}