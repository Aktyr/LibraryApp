namespace LibApp.Application.Commands.Entities.Users;

public class GetUserCommand(IUnitOfWork unitOfWork, IConverter<User, UserDTO> userConverter)
    : IGetQuery<GetUserRequest, UserResponse>, ICommand
{
    public async Task<UserResponse?> Execute(GetUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();

        // Ищем без фильтров, чтобы открыть профиль из архива
        var user = (await userRepo.ExecuteQueryIgnoringFiltersAsync(
            query => query.Where(u => u.Id == request.Id),
            cancellationToken)).FirstOrDefault();

        if (user == null) throw new UserNotFoundException();

        var userDto = userConverter.ToDto(user);
        return ResponseFactory.Single<User, UserDTO, UserResponse>(userDto);
    }
}