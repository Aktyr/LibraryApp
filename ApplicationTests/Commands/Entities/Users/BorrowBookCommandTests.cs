using LibApp.Application.Configuration;

namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class BorrowBookCommandTests
{
    private static BorrowingValidatorAsync CreateValidator(BorrowingSettings? settings = null)
    {
        var options = new Mock<IOptionsSnapshot<BorrowingSettings>>();
        options.Setup(x => x.Value).Returns(settings ?? new BorrowingSettings());
        return new BorrowingValidatorAsync(options.Object);
    }

    [Test]
    public async Task Execute_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 0,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator());
        var request = new BorrowBookRequest
        {
            UserId = Guid.NewGuid(),
            RoomBookId = roomBook.Id.Value,
            BorrowDays = 14
        };

        // Act & Assert
        Assert.ThrowsAsync<UserNotFoundException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenRoomBookNotFound_ThrowsUserRoomBookNotFoundException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        ICollection<UserRoomBook> value = [];
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = value
        };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator());
        var request = new BorrowBookRequest
        {
            UserId = user.Id.Value,
            RoomBookId = Guid.NewGuid(),
            BorrowDays = 14
        };

        // Act & Assert
        Assert.ThrowsAsync<UserRoomBookNotFoundException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenBorrowDaysLessThanMinimum_ThrowsValidationException()
    {
        // Arrange
        var settings = new BorrowingSettings { MinBorrowDays = 7, MaxBorrowDays = 30 };
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = []
        };
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 0,
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        await userRepo.AddRangeAsync([user], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator(settings));
        var request = new BorrowBookRequest
        {
            UserId = user.Id.Value,
            RoomBookId = roomBook.Id.Value,
            BorrowDays = 3 // Меньше минимума
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenBorrowDaysExceedsMaximum_ThrowsValidationException()
    {
        // Arrange
        var settings = new BorrowingSettings { MinBorrowDays = 1, MaxBorrowDays = 14 };
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = []
        };
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 0,
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        await userRepo.AddRangeAsync([user], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator(settings));
        var request = new BorrowBookRequest
        {
            UserId = user.Id.Value,
            RoomBookId = roomBook.Id.Value,
            BorrowDays = 30 // Больше максимума
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenUserExceedsMaxBooksLimit_ThrowsValidationException()
    {
        // Arrange
        var settings = new BorrowingSettings { MaxBooksPerUser = 2, MaxBorrowDays = 30 };
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks =
            [
                new() {
                    Id = new Id(Guid.NewGuid()),
                    BorrowDate = DateTime.Now.AddDays(-5),
                    Deadline = DateTime.Now.AddDays(10),
                    ReturnDate = null,
                    RoomBook = new RoomBook { Id = new Id(Guid.NewGuid()) }
                },
                new() {
                    Id = new Id(Guid.NewGuid()),
                    BorrowDate = DateTime.Now.AddDays(-3),
                    Deadline = DateTime.Now.AddDays(12),
                    ReturnDate = null,
                    RoomBook = new RoomBook { Id = new Id(Guid.NewGuid()) }
                }
            ]
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 0,
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        await userRepo.AddRangeAsync([user], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator(settings));
        var request = new BorrowBookRequest
        {
            UserId = user.Id.Value,
            RoomBookId = roomBook.Id.Value,
            BorrowDays = 14
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenUserHasOverdueBooks_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks =
            [
                new() {
                    Id = new Id(Guid.NewGuid()),
                    BorrowDate = DateTime.Now.AddDays(-30),
                    Deadline = DateTime.Now.AddDays(-5), // Просрочена
                    ReturnDate = null,
                    RoomBook = new RoomBook { Id = new Id(Guid.NewGuid()) }
                }
            ]
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 0,
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        await userRepo.AddRangeAsync([user], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator());
        var request = new BorrowBookRequest
        {
            UserId = user.Id.Value,
            RoomBookId = roomBook.Id.Value,
            BorrowDays = 14
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenNoAvailableCopies_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Test",
            FirstName = "User",
            ContactInfo = "test@test.com",
            RoomBooks = []
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 5, // Все экземпляры заняты
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        await userRepo.AddRangeAsync([user], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator());
        var request = new BorrowBookRequest
        {
            UserId = user.Id.Value,
            RoomBookId = roomBook.Id.Value,
            BorrowDays = 14
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WithValidData_BorrowsBookSuccessfully()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var userId = new Id(Guid.NewGuid());
        var roomBookId = new Id(Guid.NewGuid());

        var user = new User
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            ContactInfo = "ivanov@example.com",
            RoomBooks = []
        };

        var roomBook = new RoomBook
        {
            Id = roomBookId,
            BookCount = 5,
            BorrowedCount = 2,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        await userRepo.AddRangeAsync([user], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator());
        var request = new BorrowBookRequest
        {
            UserId = userId.Value,
            RoomBookId = roomBookId.Value,
            BorrowDays = 14
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Книга выдана"));

            // Проверяем, что UserRoomBook была создана
            var updatedUser = ((FakeRepository<User>)userRepo).Entities.First(u => u.Id.Value == userId.Value);
            Assert.That(updatedUser.RoomBooks, Has.Count.EqualTo(1));

            var userRoomBook = updatedUser.RoomBooks.First();
            Assert.That(userRoomBook.User, Is.SameAs(updatedUser));
            Assert.That(userRoomBook.RoomBook, Is.SameAs(roomBook));
            Assert.That(userRoomBook.BorrowDate, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
            Assert.That(userRoomBook.Deadline, Is.EqualTo(DateTime.UtcNow.AddDays(14)).Within(TimeSpan.FromSeconds(5)));
            Assert.That(userRoomBook.IsReturned, Is.False);

            // Проверяем, что счетчик увеличился
            Assert.That(roomBook.BorrowedCount, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Execute_WithValidData_DoesNotAffectOtherUsers()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var user1 = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            ContactInfo = "ivanov@example.com",
            RoomBooks =
        [
            new() {
                Id = new Id(Guid.NewGuid()),
                BorrowDate = DateTime.Now.AddDays(-5),
                Deadline = DateTime.Now.AddDays(10)
            }
        ]
        };

        var user2 = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Петров",
            FirstName = "Петр",
            ContactInfo = "petrov@example.com",
            RoomBooks = []
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 10,
            BorrowedCount = 3,
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        await userRepo.AddRangeAsync([user1, user2], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator());
        var request = new BorrowBookRequest
        {
            UserId = user2.Id.Value,
            RoomBookId = roomBook.Id.Value,
            BorrowDays = 14
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            // user1 не изменился
            var dbUser1 = (await userRepo.GetAsync(u => u.Id.Value == user1.Id.Value, CancellationToken.None)).First();
            Assert.That(dbUser1.RoomBooks, Has.Count.EqualTo(1));

            // user2 получил книгу
            var dbUser2 = (await userRepo.GetAsync(u => u.Id.Value == user2.Id.Value, CancellationToken.None)).First();
            Assert.That(dbUser2.RoomBooks, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Execute_WhenUserAlreadyHasBook_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var user = new User { Id = new Id(Guid.NewGuid()), LastName = "Test", FirstName = "User", ContactInfo = "test@test.com", RoomBooks = [] };
        await userRepo.AddRangeAsync([user], CancellationToken.None);

        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), BookCount = 5, BorrowedCount = 0, Book = new Book { Id = new Id(Guid.NewGuid()) }, Room = new Room { Id = new Id(Guid.NewGuid()) } };
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        // Первая выдача
        var borrow = new UserRoomBook { Id = new Id(Guid.NewGuid()), User = user, RoomBook = roomBook, BorrowDate = DateTime.UtcNow, ReturnDate = null };
        await userRoomBookRepo.AddRangeAsync([borrow], CancellationToken.None);

        var command = new BorrowBookCommand(unitOfWork, CreateValidator());
        var request = new BorrowBookRequest { UserId = user.Id.Value, RoomBookId = roomBook.Id.Value, BorrowDays = 7 };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() => command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Has.Member("Пользователь уже взял эту книгу и ещё не вернул её"));
    }

    [Test]
    public async Task Execute_WhenSaveFails_RollsBackTransaction()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var userRoomBookRepoMock = new Mock<IRepository<UserRoomBook>>();
        var roomBookRepoMock = new Mock<IRepository<RoomBook>>();

        unitOfWorkMock.Setup(u => u.GetRepository<UserRoomBook>()).Returns(userRoomBookRepoMock.Object);
        unitOfWorkMock.Setup(u => u.GetRepository<RoomBook>()).Returns(roomBookRepoMock.Object);

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = new User { Id = new Id(Guid.NewGuid()) },
            RoomBook = new RoomBook { Id = new Id(Guid.NewGuid()), BorrowedCount = 1 }
        };
        userRoomBookRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UserRoomBook, bool>>>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(userRoomBook);

        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new Exception("DB error"));

        var settingsMock = new Mock<IOptionsSnapshot<BorrowingSettings>>();
        settingsMock.Setup(s => s.Value).Returns(new BorrowingSettings());
        var validator = new BorrowingValidatorAsync(settingsMock.Object);

        var penaltyService = new PenaltyCalculatorService(Mock.Of<IOptionsSnapshot<PenaltySettings>>());
        var notificationMock = new Mock<INotificationService>();
        var command = new ReturnBookCommand(unitOfWorkMock.Object, validator, penaltyService, notificationMock.Object);
        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act & Assert
        Assert.ThrowsAsync<Exception>(() => command.Execute(request, CancellationToken.None));

        unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}