namespace LibApp.ApplicationTests.Commands.Reports;

[TestFixture]
public class GetUserActivityReportCommandTests
{
    [Test]
    public async Task Execute_WithoutFilters_ReturnsAllUsersWithActivity()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var user1 = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            Email = "ivanov@test.com",
            ContactInfo = "ivanov@test.com"
        };
        var user2 = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Петров",
            FirstName = "Петр",
            Email = "petrov@test.com",
            ContactInfo = "petrov@test.com"
        };
        var user3 = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Сидоров",
            FirstName = "Сидор",
            Email = "sidorov@test.com",
            ContactInfo = "sidorov@test.com"
        };
        await userRepo.AddRangeAsync(new[] { user1, user2, user3 }, CancellationToken.None);

        // Создаём бронирования
        var now = DateTime.UtcNow;
        var roomBook1 = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" }, Room = new Room { Id = new Id(Guid.NewGuid()) } };
        var roomBook2 = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2" }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        var borrows = new List<UserRoomBook>
        {
            // user1: 2 выдачи, 1 активная, 1 штраф 10
            new UserRoomBook
            {
                Id = new Id(Guid.NewGuid()),
                User = user1,
                RoomBook = roomBook1,
                BorrowDate = now.AddDays(-10),
                Deadline = now.AddDays(-5),
                ReturnDate = now.AddDays(-4),
                Penalty = 10
            },
            new UserRoomBook
            {
                Id = new Id(Guid.NewGuid()),
                User = user1,
                RoomBook = roomBook2,
                BorrowDate = now.AddDays(-3),
                Deadline = now.AddDays(3),
                ReturnDate = null,
                Penalty = null
            },
            // user2: 1 выдача, 0 активных, штраф 5
            new UserRoomBook
            {
                Id = new Id(Guid.NewGuid()),
                User = user2,
                RoomBook = roomBook1,
                BorrowDate = now.AddDays(-20),
                Deadline = now.AddDays(-10),
                ReturnDate = now.AddDays(-8),
                Penalty = 5
            },
            // user3: 0 выдач (активность отсутствует)
        };

        await userRoomBookRepo.AddRangeAsync(borrows, CancellationToken.None);

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest();

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(3)); // все пользователи

            var user1Data = response.Data.First(u => u.UserId == user1.Id.Value);
            Assert.That(user1Data.TotalBorrowedCount, Is.EqualTo(2));
            Assert.That(user1Data.CurrentBorrowedCount, Is.EqualTo(1));
            Assert.That(user1Data.TotalPenalty, Is.EqualTo(10));
            Assert.That(user1Data.HasOverdue, Is.False); // у user1 нет активных просрочек (дедлайн ещё в будущем)

            var user2Data = response.Data.First(u => u.UserId == user2.Id.Value);
            Assert.That(user2Data.TotalBorrowedCount, Is.EqualTo(1));
            Assert.That(user2Data.CurrentBorrowedCount, Is.EqualTo(0));
            Assert.That(user2Data.TotalPenalty, Is.EqualTo(5));
            Assert.That(user2Data.HasOverdue, Is.False);

            var user3Data = response.Data.First(u => u.UserId == user3.Id.Value);
            Assert.That(user3Data.TotalBorrowedCount, Is.EqualTo(0));
            Assert.That(user3Data.CurrentBorrowedCount, Is.EqualTo(0));
            Assert.That(user3Data.TotalPenalty, Is.EqualTo(0));
            Assert.That(user3Data.HasOverdue, Is.False);
        });
    }

    [Test]
    public async Task Execute_WithOnlyWithOverdueFilter_ReturnsOnlyUsersWithOverdue()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "User1", FirstName = "A", Email = "u1@test.com", ContactInfo = "u1" };
        var user2 = new User { Id = new Id(Guid.NewGuid()), LastName = "User2", FirstName = "B", Email = "u2@test.com", ContactInfo = "u2" };
        await userRepo.AddRangeAsync(new[] { user1, user2 }, CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()) }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        // user1 имеет активную просрочку (Deadline < now)
        var borrow1 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-20),
            Deadline = now.AddDays(-5),
            ReturnDate = null,
            Penalty = null
        };
        // user2 имеет активную книгу без просрочки
        var borrow2 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user2,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-3),
            Deadline = now.AddDays(5),
            ReturnDate = null,
            Penalty = null
        };

        await userRoomBookRepo.AddRangeAsync(new[] { borrow1, borrow2 }, CancellationToken.None);

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest { OnlyWithOverdue = true };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(1));
            Assert.That(response.Data[0].UserId, Is.EqualTo(user1.Id.Value));
            Assert.That(response.Data[0].HasOverdue, Is.True);
        });
    }

    [Test]
    public async Task Execute_WithOnlyActiveFilter_ReturnsUsersWithActiveBorrows()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "User1", FirstName = "A", Email = "u1@test.com", ContactInfo = "u1" };
        var user2 = new User { Id = new Id(Guid.NewGuid()), LastName = "User2", FirstName = "B", Email = "u2@test.com", ContactInfo = "u2" };
        await userRepo.AddRangeAsync(new[] { user1, user2 }, CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()) }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        // user1 имеет активную книгу
        var borrow1 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-3),
            Deadline = now.AddDays(5),
            ReturnDate = null
        };
        // user2 вернул все книги
        var borrow2 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user2,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-10),
            Deadline = now.AddDays(-5),
            ReturnDate = now.AddDays(-5)
        };

        await userRoomBookRepo.AddRangeAsync(new[] { borrow1, borrow2 }, CancellationToken.None);

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest { OnlyActive = true };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(1));
            Assert.That(response.Data[0].UserId, Is.EqualTo(user1.Id.Value));
            Assert.That(response.Data[0].CurrentBorrowedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Execute_WithBothFilters_ReturnsActiveAndOverdueUsers()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "User1", FirstName = "A", Email = "u1@test.com", ContactInfo = "u1" };
        var user2 = new User { Id = new Id(Guid.NewGuid()), LastName = "User2", FirstName = "B", Email = "u2@test.com", ContactInfo = "u2" };
        var user3 = new User { Id = new Id(Guid.NewGuid()), LastName = "User3", FirstName = "C", Email = "u3@test.com", ContactInfo = "u3" };
        await userRepo.AddRangeAsync(new[] { user1, user2, user3 }, CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()) }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        // user1: активная просрочка
        var borrow1 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-20),
            Deadline = now.AddDays(-5),
            ReturnDate = null
        };
        // user2: активная без просрочки
        var borrow2 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user2,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-3),
            Deadline = now.AddDays(5),
            ReturnDate = null
        };
        // user3: нет активных книг
        var borrow3 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user3,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-10),
            Deadline = now.AddDays(-5),
            ReturnDate = now.AddDays(-5)
        };

        await userRoomBookRepo.AddRangeAsync(new[] { borrow1, borrow2, borrow3 }, CancellationToken.None);

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest { OnlyWithOverdue = true, OnlyActive = true };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(1));
            Assert.That(response.Data[0].UserId, Is.EqualTo(user1.Id.Value));
        });
    }

    [Test]
    public async Task Execute_SortsByTotalPenaltyDescending()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "User1", FirstName = "A", Email = "u1@test.com", ContactInfo = "u1" };
        var user2 = new User { Id = new Id(Guid.NewGuid()), LastName = "User2", FirstName = "B", Email = "u2@test.com", ContactInfo = "u2" };
        await userRepo.AddRangeAsync(new[] { user1, user2 }, CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()) }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        var borrow1 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-5),
            Deadline = now.AddDays(-1),
            ReturnDate = now,
            Penalty = 100
        };
        var borrow2 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user2,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-5),
            Deadline = now.AddDays(-2),
            ReturnDate = now,
            Penalty = 200
        };

        await userRoomBookRepo.AddRangeAsync(new[] { borrow1, borrow2 }, CancellationToken.None);

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest();

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(2));
            Assert.That(response.Data[0].UserId, Is.EqualTo(user2.Id.Value)); // штраф 200
            Assert.That(response.Data[0].TotalPenalty, Is.EqualTo(200));
            Assert.That(response.Data[1].UserId, Is.EqualTo(user1.Id.Value));
            Assert.That(response.Data[1].TotalPenalty, Is.EqualTo(100));
        });
    }
}