namespace LibApp.Application.Commands.Auth;

public class LoginCommand(
    IUnitOfWork unitOfWork,
    JwtService jwtService,
    IAuditService auditService) : IGetQuery<LoginRequest, LoginResponse>, ICommand
{
    public async Task<LoginResponse> Execute(LoginRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();

        // Поиск без фильтра IsDeleted — иначе пользователь, удаливший себя,
        // не найдётся и получит «Неверный email или пароль» вместо предложения восстановления.
        var user = (await userRepo.ExecuteQueryIgnoringFiltersAsync(
            query => query.Where(u => u.Email == request.Email),
            cancellationToken)).FirstOrDefault();

        // Пароль проверяем до IsDeleted: иначе перебором email можно выяснить,
        // что аккаунт существует и деактивирован. Пользователь с неверным паролем
        // не должен узнать статус аккаунта.
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Неверный email или пароль");

        if (user.IsDeleted)
        {
            // Self-delete: пользователь удалил сам, может восстановиться.
            if (user.DeletedByUserId == user.Id)
            {
                return new LoginResponse(
                    Status: "RestoreRequired",
                    Message: "Аккаунт деактивирован. Восстановить?",
                    Token: null,
                    Email: user.Email,
                    Role: user.Role,
                    UserId: user.Id.Value,
                    CanSelfRestore: true);
            }

            // Удалён админом или legacy (null): восстановление только через администратора.
            throw new UnauthorizedException("Аккаунт деактивирован администратором");
        }

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