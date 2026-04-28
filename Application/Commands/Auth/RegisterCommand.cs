namespace LibApp.Application.Commands.Auth;

public class RegisterCommand : ICreateOrUpdateCommand<RegisterRequest, RegisterResponse>, ICommand
{
    private readonly IRepository<User> _userRepo;
    private readonly RegisterValidatorAsync _validator;
    private readonly IConverter<User, UserDTO> _userConverter;
    private readonly JwtService _jwtService;

    public RegisterCommand(
        IRepository<User> userRepo,
        RegisterValidatorAsync validator,
        IConverter<User, UserDTO> userConverter,
        JwtService jwtService)
    {
        _userRepo = userRepo;
        _validator = validator;
        _userConverter = userConverter;
        _jwtService = jwtService;
    }

    public async Task<RegisterResponse> Execute(RegisterRequest request, CancellationToken cancellationToken)
    {
        // Валидация
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Проверка, не занят ли email
        var existingUsers = await _userRepo.Get(u => u.Email == request.Email, cancellationToken);
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
        await _userRepo.Add(user, cancellationToken);

        // Генерируем токен
        var token = _jwtService.GenerateToken(user);

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