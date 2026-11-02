namespace LibApp.Application.Commands.Entities.Users;

/// <summary>
/// Создаёт пользователя через административную панель.
/// В отличие от самостоятельной регистрации (<see cref="RegisterCommand"/>), роль приходит из запроса
/// и может быть любой из разрешённых политикой <c>IUserManagementPolicy</c>.
/// Аудит пишется внутри <see cref="IUserService.CreateUserAsync"/> с типом <c>Create</c>.
/// </summary>
public class CreateUserCommand(IUserService userService)
    : ICreateOrUpdateCommand<CreateUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Роль приходит из запроса, но администратор может её выбрать.
        // null трактуем как Reader: клиент не обязан присылать поле.
        var role = request.Role ?? UserRole.Reader;

        await userService.CreateUserAsync(
            email: request.Email,
            password: request.Password,
            lastName: request.LastName,
            firstName: request.FirstName,
            middleName: request.MiddleName ?? "",
            contactInfo: request.ContactInfo,
            role: role,
            isSelfRegistration: false,
            cancellationToken: cancellationToken);

        return ResponseFactory.Created<User>();
    }
}