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

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "Иванов", FirstName = "Иван", Email = "ivanov@test.com", ContactInfo = "ivanov@test.com" };
        var user2 = new User { Id = new Id(Guid.NewGuid()), LastName = "Петров", FirstName = "Петр", Email = "petrov@test.com", ContactInfo = "petrov@test.com" };
        var user3 = new User { Id = new Id(Guid.NewGuid()), LastName = "Сидоров", FirstName = "Сидор", Email = "sidorov@test.com", ContactInfo = "sidorov@test.com" };

        await userRepo.AddRangeAsync([user1, user2, user3], CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook1 = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" }, Room = new Room { Id = new Id(Guid.NewGuid()) } };
        var roomBook2 = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2" }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        user1.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook1,
            BorrowDate = now.AddDays(-10),
            Deadline = now.AddDays(-5),
            ReturnDate = now.AddDays(-4),
            Penalty = 10
        });
        user1.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook2,
            BorrowDate = now.AddDays(-3),
            Deadline = now.AddDays(3),
            ReturnDate = null,
            Penalty = null
        });
        user2.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user2,
            RoomBook = roomBook1,
            BorrowDate = now.AddDays(-20),
            Deadline = now.AddDays(-10),
            ReturnDate = now.AddDays(-8),
            Penalty = 5
        });
        // user3 — без выдач

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest();

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(3));

            var user1Data = response.Data.First(u => u.UserId == user1.Id.Value);
            Assert.That(user1Data.TotalBorrowedCount, Is.EqualTo(2));
            Assert.That(user1Data.CurrentBorrowedCount, Is.EqualTo(1));
            Assert.That(user1Data.TotalPenalty, Is.EqualTo(10));
            Assert.That(user1Data.HasOverdue, Is.False);

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

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "User1", FirstName = "A", Email = "u1@test.com", ContactInfo = "u1" };
        var user2 = new User { Id = new Id(Guid.NewGuid()), LastName = "User2", FirstName = "B", Email = "u2@test.com", ContactInfo = "u2" };
        await userRepo.AddRangeAsync([user1, user2], CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()) }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        user1.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-20),
            Deadline = now.AddDays(-5),
            ReturnDate = null,
            Penalty = null
        });
        user2.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user2,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-3),
            Deadline = now.AddDays(5),
            ReturnDate = null,
            Penalty = null
        });

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

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "User1", FirstName = "A", Email = "u1@test.com", ContactInfo = "u1" };
        var user2 = new User { Id = new Id(Guid.NewGuid()), LastName = "User2", FirstName = "B", Email = "u2@test.com", ContactInfo = "u2" };
        await userRepo.AddRangeAsync([user1, user2], CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()) }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        user1.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-3),
            Deadline = now.AddDays(5),
            ReturnDate = null
        });
        user2.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user2,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-10),
            Deadline = now.AddDays(-5),
            ReturnDate = now.AddDays(-5)
        });

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

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "User1", FirstName = "A", Email = "u1@test.com", ContactInfo = "u1" };
        var user2 = new User { Id = new Id(Guid.NewGuid()), LastName = "User2", FirstName = "B", Email = "u2@test.com", ContactInfo = "u2" };
        var user3 = new User { Id = new Id(Guid.NewGuid()), LastName = "User3", FirstName = "C", Email = "u3@test.com", ContactInfo = "u3" };
        await userRepo.AddRangeAsync([user1, user2, user3], CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()) }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        user1.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-20),
            Deadline = now.AddDays(-5),
            ReturnDate = null
        });
        user2.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user2,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-3),
            Deadline = now.AddDays(5),
            ReturnDate = null
        });
        user3.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user3,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-10),
            Deadline = now.AddDays(-5),
            ReturnDate = now.AddDays(-5)
        });

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

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "User1", FirstName = "A", Email = "u1@test.com", ContactInfo = "u1" };
        var user2 = new User { Id = new Id(Guid.NewGuid()), LastName = "User2", FirstName = "B", Email = "u2@test.com", ContactInfo = "u2" };
        await userRepo.AddRangeAsync([user1, user2], CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = new Book { Id = new Id(Guid.NewGuid()) }, Room = new Room { Id = new Id(Guid.NewGuid()) } };

        user1.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user1,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-5),
            Deadline = now.AddDays(-1),
            ReturnDate = now,
            Penalty = 100
        });
        user2.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user2,
            RoomBook = roomBook,
            BorrowDate = now.AddDays(-5),
            Deadline = now.AddDays(-2),
            ReturnDate = now,
            Penalty = 200
        });

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest();

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(2));
            Assert.That(response.Data[0].UserId, Is.EqualTo(user2.Id.Value));
            Assert.That(response.Data[0].TotalPenalty, Is.EqualTo(200));
            Assert.That(response.Data[1].UserId, Is.EqualTo(user1.Id.Value));
            Assert.That(response.Data[1].TotalPenalty, Is.EqualTo(100));
        });
    }

    [Test]
    public async Task Execute_WithNoUsers_ReturnsEmptyArray()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork(); // пустой репозиторий
        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest();

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Data, Is.Empty);
        });
    }

    [Test]
    public async Task Execute_WithDateRange_FiltersAggregatesByPeriod()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();

        var user1 = new User { Id = new Id(Guid.NewGuid()), LastName = "User1", FirstName = "A", Email = "u1@test.com", ContactInfo = "u1" };
        await userRepo.AddRangeAsync([user1], CancellationToken.None);

        var day = DateTime.UtcNow.Date;
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        user1.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()), User = user1, RoomBook = roomBook,
            BorrowDate = day.AddDays(-10), Deadline = day.AddDays(-8),
            ReturnDate = day.AddDays(-9), Penalty = 10
        });
        user1.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()), User = user1, RoomBook = roomBook,
            BorrowDate = day.AddDays(-2), Deadline = day.AddDays(5),
            ReturnDate = null, Penalty = null
        });

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest { FromDate = day.AddDays(-5), ToDate = day };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert: в период попала только выдача двухдневной давности.
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(1));
            Assert.That(response.Data[0].TotalBorrowedCount, Is.EqualTo(1));
            Assert.That(response.Data[0].CurrentBorrowedCount, Is.EqualTo(1));
            Assert.That(response.Data[0].TotalPenalty, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Execute_WithDateRange_ExcludesUsersWithoutBorrowsInPeriod()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();

        var inPeriod = new User { Id = new Id(Guid.NewGuid()), LastName = "InPeriod", FirstName = "A", Email = "in@test.com", ContactInfo = "in" };
        var outOfPeriod = new User { Id = new Id(Guid.NewGuid()), LastName = "OutOfPeriod", FirstName = "B", Email = "out@test.com", ContactInfo = "out" };
        var noBorrows = new User { Id = new Id(Guid.NewGuid()), LastName = "NoBorrows", FirstName = "C", Email = "none@test.com", ContactInfo = "none" };
        await userRepo.AddRangeAsync([inPeriod, outOfPeriod, noBorrows], CancellationToken.None);

        var day = DateTime.UtcNow.Date;
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        inPeriod.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()), User = inPeriod, RoomBook = roomBook,
            BorrowDate = day.AddDays(-2), Deadline = day.AddDays(5), ReturnDate = null
        });
        outOfPeriod.RoomBooks.Add(new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()), User = outOfPeriod, RoomBook = roomBook,
            BorrowDate = day.AddDays(-30), Deadline = day.AddDays(-20), ReturnDate = day.AddDays(-25)
        });
        // noBorrows — вообще без выдач

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest { FromDate = day.AddDays(-5), ToDate = day };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert: в отчёт попадает только читатель с выдачей внутри периода.
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(1));
            Assert.That(response.Data[0].UserId, Is.EqualTo(inPeriod.Id.Value));
        });
    }

    [Test]
    public async Task Execute_WithEqualPenalty_SortsDeterministicallyByBorrowCountAndName()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRepo = unitOfWork.GetRepository<User>();

        var userA = new User { Id = new Id(Guid.NewGuid()), LastName = "Абрамов", FirstName = "A", Email = "a@test.com", ContactInfo = "a" };
        var userB = new User { Id = new Id(Guid.NewGuid()), LastName = "Борисов", FirstName = "B", Email = "b@test.com", ContactInfo = "b" };
        await userRepo.AddRangeAsync([userA, userB], CancellationToken.None);

        var now = DateTime.UtcNow;
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = new Book { Id = new Id(Guid.NewGuid()) },
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        // Одинаковый штраф, но у userA две выдачи — он должен быть выше.
        userA.RoomBooks.Add(new UserRoomBook { Id = new Id(Guid.NewGuid()), User = userA, RoomBook = roomBook, BorrowDate = now.AddDays(-5), ReturnDate = now, Penalty = 50 });
        userA.RoomBooks.Add(new UserRoomBook { Id = new Id(Guid.NewGuid()), User = userA, RoomBook = roomBook, BorrowDate = now.AddDays(-4), ReturnDate = now, Penalty = null });
        userB.RoomBooks.Add(new UserRoomBook { Id = new Id(Guid.NewGuid()), User = userB, RoomBook = roomBook, BorrowDate = now.AddDays(-3), ReturnDate = now, Penalty = 50 });

        var command = new GetUserActivityReportCommand(unitOfWork);
        var request = new GetUserActivityRequest();

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(2));
            Assert.That(response.Data[0].UserId, Is.EqualTo(userA.Id.Value));
            Assert.That(response.Data[1].UserId, Is.EqualTo(userB.Id.Value));
        });
    }
}