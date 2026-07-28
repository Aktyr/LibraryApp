namespace LibApp.Application.Commands.Entities.Users;

public class CreateUserCommand(
    IUserService userService) : ICreateOrUpdateCommand<CreateUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Роль приходит из запроса, но администратор может её выбрать
        var role = request.Role ?? UserRole.Reader; // если не указана, по умолчанию Reader

        await userService.CreateUserAsync(
            request.Email,
            request.Password,
            request.LastName,
            request.FirstName,
            request.MiddleName ?? "",
            request.ContactInfo,
            role,
            cancellationToken);

        return ResponseFactory.Created<User>();
    }
}
