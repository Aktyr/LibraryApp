namespace LibApp.Application.Commands.Auth;

public class RegisterCommand(
    IRepository<User> userRepo,
    RegisterValidatorAsync validator,
    IConverter<User, UserDTO> userConverter,
    JwtService jwtService) : ICreateOrUpdateCommand<RegisterRequest, RegisterResponse>, ICommand
{
    public async Task<RegisterResponse> Execute(RegisterRequest request, CancellationToken cancellationToken)
    {
        // Валидация
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Проверка, не занят ли email
        var existingUsers = await userRepo.Get(u => u.Email == request.Email, cancellationToken);
        if (existingUsers.Any())
            throw new LibValidationException { ExceptionDetails = new List<string> { "Email уже зарегистрирован" } };

        // Хешируем пароль
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Создаем пользователя
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = request.Email,
            PasswordHash = passwordHash,
            LastName = request.LastName,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName ?? "",
            ContactInfo = request.ContactInfo,
            Role = UserRole.Reader, // По умолчанию читатель
            RoomBooks = new List<UserRoomBook>()
        };

        // Сохраняем
        await userRepo.Add(user, cancellationToken);

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