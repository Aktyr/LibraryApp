namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class CreateUserCommandTests
{
    private CreateUserCommand CreateCommand(FakeUnitOfWork unitOfWork)
    {
        var emailValidator = new EmailValidatorAsync();
        var userRegistrationValidator = new UserValidatorAsync(emailValidator);
        var userService = new UserService(unitOfWork, userRegistrationValidator);
        return new CreateUserCommand(userService);
    }

    [TestCase("Иванов", "Иван", "Иванович", "ivanov@example.com")]
    [TestCase("Петров", "Петр", "", "petrov@example.com")] // MiddleName может быть пустым
    [TestCase("Сидорова", "Анна", "Сергеевна", "+7-999-123-45-67")]
    [TestCase("Smith", "John", "Doe", "john.smith@company.com")]
    [TestCase("A", "A", "A", "A")]
    [TestCase("A", "A", "", "A")]
    public async Task Execute_CreateUserWithValidData_CreatesUser(string lastName, string firstName, string middleName, string contactInfo)
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        await userRepo.AddRangeAsync(new Bogus.Faker<User>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.LastName, f => f.Name.LastName())
                                   .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                                   .RuleFor(x => x.MiddleName, f => f.Name.FirstName())
                                   .RuleFor(x => x.ContactInfo, f => f.Internet.Email())
                                   .RuleFor(x => x.RoomBooks, f => [])
                                   .Generate(10)
                                   .AsEnumerable());
        var createUserCommand = new CreateUserCommand(new FakeUserService(unitOfWork));
        var createUserRequest = new CreateUserRequest
        {
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            ContactInfo = contactInfo
        };

        // Act
        var response = await createUserCommand.Execute(createUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            // Проверяем, что пользователь был создан
            var users = await userRepo.GetAsync(x =>
                x.LastName == lastName &&
                x.FirstName == firstName &&
                x.MiddleName == middleName);
            Assert.That(users.Any(), Is.True);

            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("User is created."));
            Assert.That(((FakeRepository<User>)userRepo).Entities, Has.Count.EqualTo(11));
        });
    }

    [TestCase("", "Иван", "Иванович", "ivanov@example.com")] // Пустая фамилия
    [TestCase("Иванов", "", "Иванович", "ivanov@example.com")] // Пустое имя
    [TestCase("Иванов", "Иван", "Иванович", "")] // Пустой контакт
    [TestCase("", "", "", "")] 
    [TestCase("", "", "Иванович", "")] 
    [TestCase(" ", " ", "Иванович", " ")] 
    public async Task Execute_CreateUserWithInvalidData_ThrowsValidationException(
        string lastName, string firstName, string middleName, string contactInfo)
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        await userRepo.AddRangeAsync(new Bogus.Faker<User>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.LastName, f => f.Name.LastName())
                                   .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                                   .RuleFor(x => x.MiddleName, f => f.Name.FirstName())
                                   .RuleFor(x => x.ContactInfo, f => f.Internet.Email())
                                   .RuleFor(x => x.RoomBooks, f => [])
                                   .Generate(10)
                                   .AsEnumerable());

        var createUserCommand = CreateCommand(unitOfWork);

        var createUserRequest = new CreateUserRequest
        {
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            ContactInfo = contactInfo
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await createUserCommand.Execute(createUserRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_CreateUserWithLongNames_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var createUserCommand = CreateCommand(unitOfWork);

        // Генерируем слишком длинные строки (предполагая, что валидатор имеет ограничения по длине)
        var createUserRequest = new CreateUserRequest
        {
            LastName = new string('A', 101), // Слишком длинная фамилия
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov@example.com"
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await createUserCommand.Execute(createUserRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_CreateUserWithDuplicateData_StillCreatesUser()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var existingUser = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov@example.com",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([existingUser], CancellationToken.None);

        var createUserCommand = new CreateUserCommand(new FakeUserService(unitOfWork));

        // Пытаемся создать пользователя с такими же данными (допустимо, если нет ограничения на уникальность)
        var createUserRequest = new CreateUserRequest
        {
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov2@example.com" // Разный контакт
        };

        // Act
        var response = await createUserCommand.Execute(createUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            var users = await userRepo.GetAsync(x => x.LastName == "Иванов");
            Assert.That(users.Count(), Is.EqualTo(2)); // Должно быть 2 пользователя с фамилией Иванов
        });
    }

    [Test]
    public async Task Execute_CreateUser_InitializeCollectionsCorrectly()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        var createUserCommand = new CreateUserCommand(new FakeUserService(unitOfWork));
        var createUserRequest = new CreateUserRequest
        {
            LastName = "Новиков",
            FirstName = "Алексей",
            MiddleName = "Сергеевич",
            ContactInfo = "novikov@example.com"
        };

        // Act
        var response = await createUserCommand.Execute(createUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var createdUser = (await userRepo.GetAsync(x => x.LastName == "Новиков")).First();
            Assert.That(createdUser.RoomBooks, Is.Not.Null);
            Assert.That(createdUser.RoomBooks, Is.Empty); // Коллекция должна быть инициализирована пустой
        });
    }
    [Test]
    public async Task Execute_CreateUserWithTooLongMiddleName_ThrowsValidationException()
    {
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();
        await userRepo.AddRangeAsync(new Bogus.Faker<User>()
                      .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                      .RuleFor(x => x.LastName, f => f.Name.LastName())
                      .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                      .RuleFor(x => x.MiddleName, f => f.Name.FirstName())
                      .RuleFor(x => x.ContactInfo, f => f.Internet.Email())
                      .Generate(10));

        var createUserCommand = CreateCommand(unitOfWork);

        var createUserRequest = new CreateUserRequest
        {
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = new string('A', 101),
            ContactInfo = "test@test.com"
        };

        Assert.ThrowsAsync<LibValidationException>(async () =>
            await createUserCommand.Execute(createUserRequest, CancellationToken.None));
    }
    [Test]
    public async Task ValidateAsync_WhenMiddleNameTooLong_ReturnsError()
    {
        // Arrange
        var validator = new UserValidatorAsync(new EmailValidatorAsync());

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = new string('A', 101), // Слишком длинное отчество
            ContactInfo = "test@test.com"
        };

        // Act
        var result = await validator.ValidateNameAndContactAsync(
            user.LastName, 
            user.FirstName, 
            user.MiddleName, 
            user.ContactInfo);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Отчество не может превышать 100 символов"));
        });
    }
    // Тестовая реализация IUserService для использования в тестах
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
    [Test]
    public async Task Execute_WhenRoleIsNull_SetsDefaultRoleReader()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var service = new FakeUserService(unitOfWork);
        var command = new CreateUserCommand(service);

        var request = new CreateUserRequest
        {
            Email = "test@test.com",
            Password = "Password123!",
            LastName = "Иванов",
            FirstName = "Иван",
            ContactInfo = "test@test.com",
            Role = null
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        var created = (await userRepo.GetAsync(u => u.Email == "test@test.com")).First();
        Assert.That(created.Role, Is.EqualTo(UserRole.Reader));
    }
}