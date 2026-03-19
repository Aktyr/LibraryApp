using LibApp.Core.DTO.Entities;
using LibApp.Core.Exceptions.Entities;
using LibApp.Core.Responses.Entities;

namespace LibApp.ApplicationTests.Entities.Users;

[TestFixture]
public class GetUserQueryTests
{
    private IConverter<User, UserDTO> Converter => new UserDTOConverter();

    [Test]
    public async Task Execute_GetExistingUser_ReturnsUserResponse()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var users = new Bogus.Faker<User>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.MiddleName, f => f.Name.FirstName())
            .RuleFor(x => x.ContactInfo, f => f.Internet.Email())
            .RuleFor(x => x.RoomBooks, f => new List<UserRoomBook>())
            .Generate(10)
            .ToList();

        await userRepo.AddRange(users.AsEnumerable());

        var targetUser = users[4];
        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = targetUser.Id };

        // Act
        var result = await getUserQuery.Execute(getUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Status, Is.EqualTo("Ok"));
            Assert.That(result.Message, Is.EqualTo("User issued successfully."));
            Assert.That(result.Users, Has.Length.EqualTo(1));
            Assert.That(result.Users[0].Id, Is.EqualTo(targetUser.Id.Value));
            Assert.That(result.Users[0].LastName, Is.EqualTo(targetUser.LastName));
            Assert.That(result.Users[0].FirstName, Is.EqualTo(targetUser.FirstName));
            Assert.That(result.Users[0].MiddleName, Is.EqualTo(targetUser.MiddleName));
            Assert.That(result.Users[0].ContactInfo, Is.EqualTo(targetUser.ContactInfo));
            Assert.That(result.Users[0].NearestReturnTimeSpan, Is.Null); // Нет взятых книг
        });
    }

    [Test]
    public async Task Execute_GetNonExistingUser_ThrowsUserNotFoundException()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        await userRepo.AddRange(new Bogus.Faker<User>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.LastName, f => f.Name.LastName())
                                   .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                                   .RuleFor(x => x.MiddleName, f => f.Name.FirstName())
                                   .RuleFor(x => x.ContactInfo, f => f.Internet.Email())
                                   .RuleFor(x => x.RoomBooks, f => new List<UserRoomBook>())
                                   .Generate(5)
                                   .AsEnumerable());

        var nonExistingId = new Id(Guid.NewGuid());
        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = nonExistingId };

        // Act & Assert
        Assert.ThrowsAsync<UserNotFoundException>(() =>
            getUserQuery.Execute(getUserRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_GetUserFromEmptyRepository_ThrowsUserNotFoundException()
    {
        // Arrange
        var userRepo = new FakeRepository<User>(); // Пустой репозиторий
        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = new Id(Guid.NewGuid()) };

        // Act & Assert
        Assert.ThrowsAsync<UserNotFoundException>(() =>
            getUserQuery.Execute(getUserRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_GetUserWithBorrowedBooks_CalculatesNearestReturnTimeSpan()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var user = new User
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov@example.com",
            RoomBooks = new List<UserRoomBook>
            {
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BorrowDate = DateTime.Now.AddDays(-7),
                    Deadline = DateTime.Now.AddDays(3), // Через 3 дня
                    User = null,
                    RoomBook = null
                },
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BorrowDate = DateTime.Now.AddDays(-2),
                    Deadline = DateTime.Now.AddDays(1), // Через 1 день (ближайший)
                    User = null,
                    RoomBook = null
                },
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BorrowDate = DateTime.Now.AddDays(-1),
                    Deadline = null, // Без дедлайна
                    User = null,
                    RoomBook = null
                }
            }
        };

        await userRepo.AddRange([user]);

        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = userId };

        // Act
        var result = await getUserQuery.Execute(getUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Users, Has.Length.EqualTo(1));
            var userDTO = result.Users[0];

            // Ближайший дедлайн должен быть ~1 день
            Assert.That(userDTO.NearestReturnTimeSpan, Is.Not.Null);
            Assert.That(userDTO.NearestReturnTimeSpan!.Value.TotalDays, Is.EqualTo(1).Within(0.1));
        });
    }

    [Test]
    public async Task Execute_GetUserWithoutBorrowedBooks_ReturnsNullNearestReturnTimeSpan()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var user = new User
        {
            Id = userId,
            LastName = "Петров",
            FirstName = "Петр",
            MiddleName = "",
            ContactInfo = "petrov@example.com",
            RoomBooks = new List<UserRoomBook>() // Пустой список
        };

        await userRepo.AddRange([user]);

        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = userId };

        // Act
        var result = await getUserQuery.Execute(getUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Users, Has.Length.EqualTo(1));
            Assert.That(result.Users[0].NearestReturnTimeSpan, Is.Null);
        });
    }

    [Test]
    public async Task Execute_GetUserWithOnlyBooksWithoutDeadline_ReturnsNullNearestReturnTimeSpan()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var user = new User
        {
            Id = userId,
            LastName = "Сидоров",
            FirstName = "Сидор",
            MiddleName = "Сидорович",
            ContactInfo = "sidorov@example.com",
            RoomBooks = new List<UserRoomBook>
            {
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BorrowDate = DateTime.Now.AddDays(-5),
                    Deadline = null, // Без дедлайна
                    User = null,
                    RoomBook = null
                },
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BorrowDate = DateTime.Now.AddDays(-10),
                    Deadline = null, // Без дедлайна
                    User = null,
                    RoomBook = null
                }
            }
        };

        await userRepo.AddRange([user]);

        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = userId };

        // Act
        var result = await getUserQuery.Execute(getUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Users, Has.Length.EqualTo(1));
            Assert.That(result.Users[0].NearestReturnTimeSpan, Is.Null);
        });
    }

    [Test]
    public async Task Execute_GetUser_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());
        var specificGuid = Guid.Parse("12345678-1234-1234-1234-123456789012");

        var user = new User
        {
            Id = new Id(specificGuid),
            LastName = "Тестов",
            FirstName = "Тест",
            MiddleName = "Тестович",
            ContactInfo = "test@test.test",
            RoomBooks = []
        };

        await userRepo.AddRange([user]);

        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = new Id(specificGuid) };

        // Act
        var result = await getUserQuery.Execute(getUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            var userDTO = result!.Users[0];

            Assert.That(userDTO.Id, Is.EqualTo(specificGuid));
            Assert.That(userDTO.LastName, Is.EqualTo("Тестов"));
            Assert.That(userDTO.FirstName, Is.EqualTo("Тест"));
            Assert.That(userDTO.MiddleName, Is.EqualTo("Тестович"));
            Assert.That(userDTO.ContactInfo, Is.EqualTo("test@test.test"));
        });
    }

    [Test]
    public async Task Execute_GetUser_ReturnsUserInArray()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var user = new User
        {
            Id = userId,
            LastName = "Массивов",
            FirstName = "Аррей",
            MiddleName = "Листов",
            ContactInfo = "array@list.com",
            RoomBooks = []
        };

        await userRepo.AddRange([user]);

        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = userId };

        // Act
        var result = await getUserQuery.Execute(getUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Users, Is.InstanceOf<UserDTO[]>());
            Assert.That(result.Users, Has.Length.EqualTo(1));
            Assert.That(result.Users[0], Is.InstanceOf<UserDTO>());
        });
    }

    [Test]
    public async Task Execute_GetUser_ReturnsCorrectResponseType()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Тестов",
            FirstName = "Пользователь",
            ContactInfo = "test@test.com"
        };

        await userRepo.AddRange([user]);

        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = user.Id };

        // Act
        var result = await getUserQuery.Execute(getUserRequest, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<UserResponse>());
    }

    [Test]
    public async Task Execute_GetUser_PassesCancellationToken()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var userId = new Id(Guid.NewGuid());

        var user = new User
        {
            Id = userId,
            LastName = "Токенов",
            FirstName = "Кансел",
            ContactInfo = "cancel@token.ru"
        };

        await userRepo.AddRange([user]);

        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = userId };
        var cancellationToken = new CancellationToken();

        // Act
        var result = await getUserQuery.Execute(getUserRequest, cancellationToken);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo("Ok"));
    }

    [Test]
    public async Task Execute_GetUser_WithMultipleUsersHavingSameData_ReturnsCorrectUser()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();

        var user1 = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov1@example.com"
        };

        var user2 = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov2@example.com" // Разный контакт
        };

        await userRepo.AddRange([user1, user2]);

        var getUserQuery = new GetUserQuery(userRepo, Converter);
        var getUserRequest = new GetUserRequest { Id = user2.Id };

        // Act
        var result = await getUserQuery.Execute(getUserRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Users[0].Id, Is.EqualTo(user2.Id.Value));
            Assert.That(result.Users[0].ContactInfo, Is.EqualTo("ivanov2@example.com"));
        });
    }
}