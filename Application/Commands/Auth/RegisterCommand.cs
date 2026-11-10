namespace LibApp.Application.Commands.Auth;

[NoAudit("Audit is written in UserService.CreateUserAsync")]
public class RegisterCommand(
    IUserService userService,
    JwtService jwtService) : ICreateOrUpdateCommand<RegisterRequest, RegisterResponse>, ICommand
{
    public async Task<RegisterResponse> Execute(RegisterRequest request, CancellationToken cancellationToken)
    {
        // Создаём пользователя через сервис
        var user = await userService.CreateUserAsync(
            email: request.Email,
            password: request.Password,
            lastName: request.LastName,
            firstName: request.FirstName,
            middleName: request.MiddleName ?? "",
            contactInfo: request.ContactInfo,
            role: UserRole.Reader,
            isSelfRegistration: true,
            cancellationToken: cancellationToken);

        // Генерируем токен
        var token = jwtService.GenerateToken(user);

        return new RegisterResponse(
            Status: "Ok",
            Message: "Регистрация успешна",
            Token: token,
            Email: user.Email,
            Role: user.Role,
            UserId: user.Id.Value
        );
    }
}
