namespace LibApp.Application.Commands.Entities.Users;

public class GetAllUsersCommand(IUnitOfWork unitOfWork, IConverter<User, UserDTO> userConverter)
    : IGetQuery<EmptyRequest, UserResponse>, ICommand
{
    public async Task<UserResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var users = await userRepo.GetWithoutTrackingAsync(cancellationToken);
        var userDTOs = users.Select(user => userConverter.ToDto(user)).ToArray();

        return ResponseFactory.List<User, UserDTO, UserResponse>(userDTOs);
    }
}