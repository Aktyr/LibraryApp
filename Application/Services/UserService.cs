public class UserService : IUserService, IService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserValidatorAsync _validator;
    private readonly IAuditService _auditService;

    public UserService(IUnitOfWork unitOfWork, UserValidatorAsync validator, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _auditService = auditService;
    }

    public async Task<User> CreateUserAsync(
        string email,
        string password,
        string lastName,
        string firstName,
        string middleName,
        string contactInfo,
        UserRole role,
        bool isSelfRegistration = false,
        CancellationToken cancellationToken = default)
    {
        // Валидация
        var validationResult = await _validator.ValidateAllAsync(
            email, password, lastName, firstName, middleName, contactInfo, cancellationToken);

        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Проверка уникальности email
        var userRepo = _unitOfWork.GetRepository<User>();
        if (await userRepo.AnyAsync(u => u.Email == email, cancellationToken))
            throw new LibValidationException { ExceptionDetails = ["Email уже зарегистрирован"] };

        // Хеширование
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = email,
            PasswordHash = passwordHash,
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName ?? string.Empty,
            ContactInfo = contactInfo,
            Role = role,
            SubscriptionStatus = SubscriptionStatus.Active,
            RoomBooks = []
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await userRepo.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Тип действия зависит от источника вызова: регистрация или создание админом.
            var action = isSelfRegistration ? AuditActionType.Register : AuditActionType.Create;
            var details = isSelfRegistration
                ? $"Самостоятельная регистрация: «{user.FullName}» ({user.Email})"
                : $"Создан пользователь «{user.FullName}» ({user.Email}, роль {user.Role})";

            await _auditService.LogAsync(
                action,
                nameof(User),
                user.Id.Value,
                details: details,
                userId: user.Id,
                userName: user.FullName,
                ct: cancellationToken);
        }, cancellationToken);

        return user;
    }
}