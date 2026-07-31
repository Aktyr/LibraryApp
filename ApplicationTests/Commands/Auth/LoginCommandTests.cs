namespace LibApp.ApplicationTests.Commands.Auth;

[TestFixture]
public class LoginCommandTests
{
    // private IConverter<User, UserDTO> Converter => new UserDTOConverter();
    // Потребуется добавить: using Microsoft.Extensions.Options;
    private JwtService JwtService => new(Options.Create(new JwtSettings
    {
        SecretKey = "test-secret-key-for-testing-purposes-only-12345",
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpiryMinutes = 60
    }));

    [Test]
    public async Task Execute_WithValidData_ReturnsSuccessResponse()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();

        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "Password";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Id = new Id(userId),
            Email = email,
            PasswordHash = passwordHash,
            Role = UserRole.Reader,
            LastName = "Test",
            FirstName = "User",
            ContactInfo = email,
            RoomBooks = []
        };

        await userRepo.AddRangeAsync([user], CancellationToken.None);

        var loginCommand = new LoginCommand(unitOfWork, JwtService);
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await loginCommand.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Message, Is.EqualTo("Вход выполнен успешно"));
            Assert.That(result.Token, Is.Not.Empty);
            Assert.That(result.Email, Is.EqualTo(email));
            Assert.That(result.Role, Is.EqualTo(UserRole.Reader));
            Assert.That(result.UserId, Is.EqualTo(userId));
        });

        var users = await userRepo.GetAsync(x => x.Email == email, CancellationToken.None);
        Assert.That(users.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Execute_WithInvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();

        var email = "test@example.com";
        var password = "Password";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = email,
            PasswordHash = passwordHash,
            Role = UserRole.Reader,
            LastName = "Test",
            FirstName = "User",
            ContactInfo = email,
            RoomBooks = []
        };

        await userRepo.AddRangeAsync([user], CancellationToken.None);

        var loginCommand = new LoginCommand(unitOfWork, JwtService);
        var request = new LoginRequest
        {
            Email = email,
            Password = "wrongPassword"
        };

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(() =>
            loginCommand.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WithNonExistentEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "existing@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            LastName = "Test",
            FirstName = "User",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        var loginCommand = new LoginCommand(unitOfWork, JwtService);
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "anyPassword"
        };

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(() =>
            loginCommand.Execute(request, CancellationToken.None));
    }

    [TestCase("")]
    //[TestCase(null)]
    public async Task Execute_WithEmptyEmail_ThrowsUnauthorizedException(string email)
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            LastName = "Test",
            FirstName = "User",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        var loginCommand = new LoginCommand(unitOfWork, JwtService);
        var request = new LoginRequest
        {
            Email = email,
            Password = "password"
        };

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(() =>
            loginCommand.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WithNullEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            LastName = "Test",
            FirstName = "User",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        var loginCommand = new LoginCommand(unitOfWork, JwtService);
        var request = new LoginRequest
        {
            Email = null!,
            Password = "password"
        };

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(() =>
            loginCommand.Execute(request, CancellationToken.None));
    }


    [Test]
    public async Task Execute_WithEmptyPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();

        var email = "test@example.com";
        var password = "correctPassword";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = email,
            PasswordHash = passwordHash,
            LastName = "Test",
            FirstName = "User",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        var loginCommand = new LoginCommand(unitOfWork, JwtService);
        var request = new LoginRequest
        {
            Email = email,
            Password = ""
        };

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(() =>
            loginCommand.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WithNullPassword_ThrowsArgumentNullException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = (FakeRepository<User>)unitOfWork.GetRepository<User>();

        var email = "test@example.com";
        var password = "correctPassword";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = email,
            PasswordHash = passwordHash,
            LastName = "Test",
            FirstName = "User",
            RoomBooks = []
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        var loginCommand = new LoginCommand(unitOfWork, JwtService);
        var request = new LoginRequest
        {
            Email = email,
            Password = null!
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            loginCommand.Execute(request, CancellationToken.None));
    }
}