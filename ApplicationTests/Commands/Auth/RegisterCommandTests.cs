namespace LibApp.ApplicationTests.Commands.Auth;

[TestFixture]
public class RegisterCommandTests
{
    private RegisterValidatorAsync CreateRegisterValidator => new();
    private IConverter<User, UserDTO> Converter => new UserDTOConverter();

    private RegisterCommand CreateCommand(FakeUnitOfWork unitOfWork, JwtService jwtService)
    {
        var emailValidator = new EmailValidatorAsync();
        var userRegistrationValidator = new UserRegistrationValidator(emailValidator);
        var userService = new UserService(unitOfWork, userRegistrationValidator);
        return new RegisterCommand(userService, jwtService);
    }

    private JwtService JwtService => new(Options.Create(new JwtSettings
    {
        SecretKey = "test-secret-key-for-testing-purposes-only-12345",
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpiryMinutes = 60
    }));

    [TestCase("test@test.com", "Password123!", "Иванов", "Иван", "Иванович", "+7-999-123-45-67")]
    [TestCase("test@test.com", "123456", "Петров", "Петр", "", "test@test.com")]
    public async Task Execute_WithValidData_CreatesUserAndReturnsResponse(
        string email, string password, string lastName, string firstName, string middleName, string contactInfo)
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        await userRepo.AddRangeAsync(new Bogus.Faker<User>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .Generate(5)
            .AsEnumerable(), CancellationToken.None);

        var registerCommand = new RegisterCommand(new FakeUserService(unitOfWork), JwtService);
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = password,
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            ContactInfo = contactInfo
        };

        // Act
        var response = await registerCommand.Execute(registerRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Регистрация успешна"));
            Assert.That(response.Token, Is.Not.Empty);
            Assert.That(response.Email, Is.EqualTo(email));
            Assert.That(response.Role, Is.EqualTo(UserRole.Reader));

            var users = await userRepo.GetAsync(x => x.Email == email); // Получаем список почт
            Assert.That(users.Any(), Is.True);
            var user = users.First(); // Получаем нового пользователя
            Assert.That(user.LastName, Is.EqualTo(lastName));
            Assert.That(user.FirstName, Is.EqualTo(firstName));
            Assert.That(user.PasswordHash, Is.Not.EqualTo(password));
            Assert.That(userRepo.Entities, Has.Count.EqualTo(6)); // 5 старых + 1 новый
        });
    }

    // В тестовом проекте создаём фейковую реализацию IUserService
    private class FakeUserService : IUserService
    {
        private readonly FakeUnitOfWork _uow;
        public FakeUserService(FakeUnitOfWork uow) => _uow = uow;

        public async Task<User> CreateUserAsync(string email, string password, string lastName, string firstName, string middleName, string contactInfo, UserRole role, CancellationToken cancellationToken = default)
        {
            var repo = (FakeRepository<User>)_uow.GetRepository<User>();
            var user = new User
            {
                Id = new Id(Guid.NewGuid()),
                Email = email ?? string.Empty,
                LastName = lastName,
                FirstName = firstName,
                MiddleName = middleName,
                ContactInfo = contactInfo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password ?? "password"),
                Role = role,
                RoomBooks = []
            };

            await repo.AddRangeAsync([user], cancellationToken);
            return user;
        }
    }

    [TestCase("existing@example.com", "Password123!", "Иванов", "Иван")]
    public async Task Execute_WithDuplicateEmail_ThrowsValidationException(
        string email, string password, string lastName, string firstName)
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var existingUser = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            LastName = "Existing",
            FirstName = "User",
            ContactInfo = email
        };
        await userRepo.AddRangeAsync([existingUser], CancellationToken.None);

        var registerCommand = CreateCommand(unitOfWork, JwtService);
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = password,
            LastName = lastName,
            FirstName = firstName,
            ContactInfo = email
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(async () =>
            await registerCommand.Execute(registerRequest, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Contains.Item("Email уже зарегистрирован"));
    }

    [TestCase("", "Password123!", "Иванов", "Иван")]                // Пустой email
    [TestCase("invalid-email", "Password123!", "Иванов", "Иван")]   // Невалидный email
    [TestCase("test@test.com", "", "Иванов", "Иван")]               // Пустой пароль
    [TestCase("test@test.com", "123", "Иванов", "Иван")]            // Слишком короткий пароль
    [TestCase("test@test.com", "Password123!", "", "Иван")]         // Пустая фамилия
    [TestCase("test@test.com", "Password123!", "Иванов", "")]       // Пустое имя
    [TestCase("invalid-email", "", " ", "")]       
    [TestCase("test@test.com", "", "", "")]       
    [TestCase("", "Password123", "", "")]       
    [TestCase("", "", "Иванов", "")]       
    [TestCase("", "", "", "Иван")]       
    [TestCase(" ", " ", " ", " ")]       
    [TestCase("", "", "", "")]       
    public async Task Execute_WithInvalidData_ThrowsValidationException(
        string email, string password, string lastName, string firstName)
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        await userRepo.AddRangeAsync(new Bogus.Faker<User>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .Generate(3)
            .AsEnumerable(), CancellationToken.None);

        var registerCommand = CreateCommand(unitOfWork, JwtService);

        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = password,
            LastName = lastName,
            FirstName = firstName,
            ContactInfo = "test"
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await registerCommand.Execute(registerRequest, CancellationToken.None));
    }
    [Test]
    public async Task Execute_WithTooLongPassword_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        await userRepo.AddRangeAsync(new Bogus.Faker<User>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .Generate(3)
            .AsEnumerable(), CancellationToken.None);

        var registerCommand = CreateCommand(unitOfWork, JwtService);
        var registerRequest = new RegisterRequest
        {
            Email = "test@test.com",
            Password = new string('A', 101),
            LastName = "Иванов",
            FirstName = "Иван",
            ContactInfo = "test@test.com"
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await registerCommand.Execute(registerRequest, CancellationToken.None));
    }
    [Test]
    public async Task ValidateAsync_WhenContactInfoIsEmpty_ReturnsError()
    {
        // Arrange
        var validator = new RegisterValidatorAsync();
        var request = new RegisterRequest
        {
            Email = "test@test.com",
            Password = "Password123!",
            LastName = "Иванов",
            FirstName = "Иван",
            ContactInfo = "" // Пустой контакт
        };

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Контактная информация обязательна"));
        });
    }
}