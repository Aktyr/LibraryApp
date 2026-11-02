namespace LibApp.Application.Commands.Auth;

public class LoginCommand(
    IUnitOfWork unitOfWork,
    JwtService jwtService,
    IAuditService auditService) : IGetQuery<LoginRequest, LoginResponse>, ICommand
{
    public async Task<LoginResponse> Execute(LoginRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();

        var user = await userRepo.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || user.IsDeleted)
            throw new UnauthorizedException("Неверный email или пароль");

        // Проверка пароля
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Неверный email или пароль");

        // Генерация токена
        var token = jwtService.GenerateToken(user);

        // Логин не меняет состояние пользователя, поэтому транзакция не нужна —
        // достаточно одной вставки в журнал и одного SaveChanges.
        // userId/userName передаём явно: ICurrentUserService ещё не знает о пользователе.
        await auditService.LogAsync(
            AuditActionType.Login,
            nameof(User),
            user.Id.Value,
            details: $"Вход в систему: {user.Email}",
            userId: user.Id,
            userName: user.FullName,
            ct: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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