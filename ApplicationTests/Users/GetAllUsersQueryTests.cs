using LibApp.Core.Responses.User;
using Moq;

namespace LibApp.ApplicationTests.Users;

[TestFixture]
public class GetAllUsersQueryTests
{
    [Test]
    public async Task Execute_GetAllUsersFromEmptyRepository_ReturnsEmptyArray()
    {
        // Arrange
        var userRepo = new FakeRepository<User>(); // Пустой репозиторий
        var getAllUsersQuery = new GetAllUsersQuery(userRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllUsersQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Message, Is.EqualTo("List of users issued successfully."));
            Assert.That(result.Users, Is.Not.Null);
            Assert.That(result.Users, Is.Empty);
        });
    }

    [Test]
    public async Task Execute_GetAllUsers_ReturnsAllUsers()
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
            .Generate(15)
            .ToList();

        await userRepo.AddRange(users.AsEnumerable());

        var getAllUsersQuery = new GetAllUsersQuery(userRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllUsersQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Users, Has.Length.EqualTo(15));
            Assert.That(result.Users.Select(u => u.Id), Is.EquivalentTo(users.Select(u => u.Id.Value)));
        });
    }

    [Test]
    public async Task Execute_GetAllUsers_ReturnsCorrectUserData()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();

        var user1 = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov@example.com",
            RoomBooks = []
        };

        var user2 = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Петрова",
            FirstName = "Анна",
            MiddleName = "", // Пустое отчество
            ContactInfo = "petrova@company.com",
            RoomBooks = []
        };

        await userRepo.AddRange([user1, user2]);

        var getAllUsersQuery = new GetAllUsersQuery(userRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllUsersQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Users, Has.Length.EqualTo(2));

            // Проверяем первого пользователя
            var resultUser1 = result.Users.FirstOrDefault(u => u.Id == user1.Id.Value);
            Assert.That(resultUser1, Is.Not.Null);
            Assert.That(resultUser1.LastName, Is.EqualTo("Иванов"));
            Assert.That(resultUser1.FirstName, Is.EqualTo("Иван"));
            Assert.That(resultUser1.MiddleName, Is.EqualTo("Иванович"));
            Assert.That(resultUser1.ContactInfo, Is.EqualTo("ivanov@example.com"));
            Assert.That(resultUser1.NearestReturnTimeSpan, Is.Null); // Нет взятых книг

            // Проверяем второго пользователя
            var resultUser2 = result.Users.FirstOrDefault(u => u.Id == user2.Id.Value);
            Assert.That(resultUser2, Is.Not.Null);
            Assert.That(resultUser2.LastName, Is.EqualTo("Петрова"));
            Assert.That(resultUser2.FirstName, Is.EqualTo("Анна"));
            Assert.That(resultUser2.MiddleName, Is.EqualTo(""));
            Assert.That(resultUser2.ContactInfo, Is.EqualTo("petrova@company.com"));
        });
    }

    [Test]
    public async Task Execute_GetAllUsersWithBorrowedBooks_CalculatesNearestReturnTimeSpan()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();

        var userId = new Id(Guid.NewGuid());
        var user = new User
        {
            Id = userId,
            LastName = "Сидоров",
            FirstName = "Алексей",
            MiddleName = "Владимирович",
            ContactInfo = "sidorov@test.ru",
            RoomBooks = new List<UserRoomBook>
            {
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    Issue = DateTime.Now.AddDays(-10),
                    Deadline = DateTime.Now.AddDays(2), // Через 2 дня
                    User = null,
                    RoomBook = null
                },
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    Issue = DateTime.Now.AddDays(-5),
                    Deadline = DateTime.Now.AddDays(5), // Через 5 дней
                    User = null,
                    RoomBook = null
                },
                new UserRoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    Issue = DateTime.Now.AddDays(-3),
                    Deadline = null, // Без дедлайна
                    User = null,
                    RoomBook = null
                }
            }
        };

        await userRepo.AddRange([user]);

        var getAllUsersQuery = new GetAllUsersQuery(userRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllUsersQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Users, Has.Length.EqualTo(1));
            var resultUser = result.Users[0];

            // NearestReturnTimeSpan должен быть ~2 дня (ближайший дедлайн)
            Assert.That(resultUser.NearestReturnTimeSpan, Is.Not.Null);
            Assert.That(resultUser.NearestReturnTimeSpan!.Value.TotalDays, Is.EqualTo(2).Within(0.1));
        });
    }

    [Test]
    public async Task Execute_GetAllUsers_UsersAreSortedAsInRepository()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();

        var users = new List<User>
        {
            new User { Id = new Id(Guid.NewGuid()), LastName = "Абрамов", FirstName = "Алексей", ContactInfo = "a@test.ru" },
            new User { Id = new Id(Guid.NewGuid()), LastName = "Борисов", FirstName = "Борис", ContactInfo = "b@test.ru" },
            new User { Id = new Id(Guid.NewGuid()), LastName = "Васильев", FirstName = "Василий", ContactInfo = "c@test.ru" },
        };

        await userRepo.AddRange(users);

        var getAllUsersQuery = new GetAllUsersQuery(userRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllUsersQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.That(result.Users.Select(u => u.LastName),
            Is.EqualTo(new[] { "Абрамов", "Борисов", "Васильев" }));
    }

    [Test]
    public async Task Execute_GetAllUsers_UsesGetWithoutTracking()
    {
        // Arrange
        var mockUserRepo = new Mock<IRepository<User>>();
        var users = new List<User>
        {
            new User { Id = new Id(Guid.NewGuid()), LastName = "Test", FirstName = "User", ContactInfo = "test@test.ru" }
        };

        mockUserRepo.Setup(repo => repo.GetWithoutTracking(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(users);

        var getAllUsersQuery = new GetAllUsersQuery(mockUserRepo.Object);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllUsersQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        mockUserRepo.Verify(repo => repo.GetWithoutTracking(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result.Users, Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Execute_GetAllUsers_HandlesCancellationToken()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        await userRepo.AddRange(new Bogus.Faker<User>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .Generate(3)
            .AsEnumerable());

        var getAllUsersQuery = new GetAllUsersQuery(userRepo);
        var emptyRequest = new EmptyRequest();
        var cancellationToken = new CancellationToken();

        // Act
        var result = await getAllUsersQuery.Execute(emptyRequest, cancellationToken);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Users, Has.Length.EqualTo(3));
    }

    [Test]
    public async Task Execute_GetAllUsers_ReturnsCorrectResponseType()
    {
        // Arrange
        var userRepo = new FakeRepository<User>();
        var getAllUsersQuery = new GetAllUsersQuery(userRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllUsersQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<UserResponse>());
        Assert.That(result, Is.TypeOf<UserResponse>());
    }
}