namespace LibApp.Application.Commands.Auth;

public class LoginCommand(IUnitOfWork unitOfWork, JwtService jwtService) : IGetQuery<LoginRequest, LoginResponse>, ICommand
{
    public async Task<LoginResponse> Execute(LoginRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();

        // Поиск пользователя по email
        var users = await userRepo.GetAsync(u => u.Email == request.Email, cancellationToken);
        var user = users.FirstOrDefault();

        if (user == null)
            throw new UnauthorizedException("Неверный email или пароль");

        // Проверка пароля
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Неверный email или пароль");

        // Генерация токена
        var token = jwtService.GenerateToken(user);

        return new LoginResponse(
            Status: "Ok",
            Message: "Вход выполнен успешно",
            Token: token,
            Email: user.Email,
            Role: user.Role,
            UserId: user.Id.Value
        );
    }
}