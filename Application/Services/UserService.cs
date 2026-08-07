namespace LibApp.Application.Services;

public class UserService : IUserService, IService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserValidatorAsync _validator;

    public UserService(IUnitOfWork unitOfWork, UserValidatorAsync validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<User> CreateUserAsync(
        string email,
        string password,
        string lastName,
        string firstName,
        string middleName,
        string contactInfo,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        // Валидация
        var validationResult = await _validator.ValidateAllAsync(
            email, 
            password, 
            lastName, 
            firstName, 
            middleName, 
            contactInfo, 
            cancellationToken);

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
            RoomBooks = []
        };

        // Сохранение в транзакции (на случай добавления связанных данных)
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await userRepo.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return user;
    }
}