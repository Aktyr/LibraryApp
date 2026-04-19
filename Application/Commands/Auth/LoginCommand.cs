namespace LibApp.Application.Commands.Auth;

public class LoginCommand : IGetQuery<LoginRequest, LoginResponse>, ICommand
{
    private readonly IRepository<User> _userRepo;
    private readonly JwtService _jwtService;

    public LoginCommand(IRepository<User> userRepo, JwtService jwtService)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> Execute(LoginRequest request, CancellationToken cancellationToken)
    {
        // Поиск пользователя по email
        var users = await _userRepo.Get(u => u.Email == request.Email, cancellationToken);
        var user = users.FirstOrDefault();

        if (user == null)
            throw new UnauthorizedException("Неверный email или пароль");

        // Проверка пароля
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Неверный email или пароль");

        // Генерация токена
        var token = _jwtService.GenerateToken(user);

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