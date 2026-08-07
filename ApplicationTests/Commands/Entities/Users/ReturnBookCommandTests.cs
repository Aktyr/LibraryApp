namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class ReturnBookCommandTests
{
    private PenaltyCalculatorService CreatePenaltyService(PenaltySettings? settings = null)
    {
        var optionsMock = new Mock<IOptionsSnapshot<PenaltySettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings ?? new PenaltySettings());
        return new PenaltyCalculatorService(optionsMock.Object);
    }

    private BorrowingValidatorAsync CreateValidator(BorrowingSettings? settings = null)
    {
        var optionsMock = new Mock<IOptionsSnapshot<BorrowingSettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings ?? new BorrowingSettings());
        return new BorrowingValidatorAsync(optionsMock.Object);
    }

    [Test]
    public async Task Execute_WhenUserRoomBookNotFound_ThrowsException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = Guid.NewGuid() };

        // Act & Assert
        Assert.ThrowsAsync<UserRoomBookNotFoundException>(() =>
            command.Execute(request, CancellationToken.None));
    }
    [Test]
    public async Task Execute_WhenBookAlreadyReturned_ThrowsException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            ReturnDate = DateTime.Now.AddDays(-1),  // Уже возвращена
            RoomBook = new RoomBook { Id = new Id(Guid.NewGuid()) }
        };
        await userRoomBookRepo.AddRangeAsync(new[] { userRoomBook }, CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork, CreateValidator(), CreatePenaltyService(), notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));

    }
    [Test]
    public async Task Execute_WhenReturnedWithoutOverdue_NoPenaltyApplied()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 2,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = DateTime.Now.AddDays(-5),
            Deadline = DateTime.Now.AddDays(5),   // Дедлайн в будущем
            ReturnDate = null,
            Penalty = null,
            User = new User { Id = new Id(Guid.NewGuid()), Email = "user@test.com" },
            RoomBook = roomBook
        };

        await userRoomBookRepo.AddRangeAsync(new[] { userRoomBook }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new ReturnBookCommand(unitOfWork, CreateValidator(), CreatePenaltyService(), notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Книга возвращена"));
            Assert.That(response.Message, Does.Not.Contain("Штраф"));

            var updatedUrb = userRoomBookRepo.Entities.First();
            Assert.That(updatedUrb.ReturnDate, Is.Not.Null);
            Assert.That(updatedUrb.IsReturned, Is.True);
            Assert.That(updatedUrb.Penalty, Is.Null);
            Assert.That(roomBook.BorrowedCount, Is.EqualTo(1));

            notificationMock.Verify(
                x => x.SendOverdueNotificationAsync(
                    It.IsAny<User>(), It.IsAny<UserRoomBook>(),
                    It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
                Times.Never);
        });
    }
    [Test]
    public async Task Execute_WhenReturnedWithOverdue_PenaltyAppliedAndNotificationSent()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var penaltySettings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 0 };
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 2,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Overdue Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        var user = new User { Id = new Id(Guid.NewGuid()), Email = "user@test.com" };

        // 5 дней просрочки
        var deadline = new DateTime(2024, 1, 5, 12, 0, 0);
        var currentDate = new DateTime(2024, 1, 10, 12, 0, 0);

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = deadline.AddDays(-14),
            Deadline = deadline,
            ReturnDate = null,
            Penalty = null,
            User = user,
            RoomBook = roomBook
        };

        await userRoomBookRepo.AddRangeAsync(new[] { userRoomBook }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(penaltySettings),
            notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Штраф"));

            var updatedUrb = userRoomBookRepo.Entities.First();
            Assert.That(updatedUrb.ReturnDate, Is.Not.Null);
            Assert.That(updatedUrb.IsReturned, Is.True);

            // Проверяем, что Penalty > 0 и кратен DailyRate
            Assert.That(updatedUrb.Penalty, Is.GreaterThan(0));
            Assert.That(updatedUrb.Penalty % 10, Is.EqualTo(0));

            Assert.That(roomBook.BorrowedCount, Is.EqualTo(1));

            // Проверяем, что уведомление было отправлено (не проверяем точную сумму)
            notificationMock.Verify(x => x.SendOverdueNotificationAsync( 
                user, userRoomBook, It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Once);
        });
    }
    [Test]
    public async Task Execute_WhenPenaltyIsZero_NoNotificationSent()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 1,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "On Time Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        var user = new User { Id = new Id(Guid.NewGuid()), Email = "user@test.com" };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = DateTime.Now.AddDays(-14),
            Deadline = DateTime.Now.AddDays(1), // Дедлайн еще не наступил
            ReturnDate = null,
            Penalty = null,
            User = user,
            RoomBook = roomBook
        };

        await userRoomBookRepo.AddRangeAsync(new[] { userRoomBook }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Книга возвращена"));

            var updatedUrb = userRoomBookRepo.Entities.First();
            Assert.That(updatedUrb.Penalty, Is.Null.Or.EqualTo(0m));

            // Проверяем, что уведомление о штрафе не отправлялось
            notificationMock.Verify(x => x.SendOverdueNotificationAsync(
                It.IsAny<User>(), It.IsAny<UserRoomBook>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
                Times.Never);
        });
    }

    [Test]
    public async Task Execute_WhenOverdueWithMaxPenalty_PenaltyCapped()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        // Настраиваем лимит штрафа
        var penaltySettings = new PenaltySettings { DailyRate = 10m, MaxPenalty = 150m, GracePeriodDays = 0 };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 1,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Very Overdue Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        var user = new User { Id = new Id(Guid.NewGuid()), Email = "user@test.com" };

        // Делаем просрочку 20 дней (20 * 10 = 200, что больше лимита 150)
        var deadline = DateTime.Now.AddDays(-20);

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = deadline.AddDays(-14),
            Deadline = deadline,
            ReturnDate = null,
            Penalty = null,
            User = user,
            RoomBook = roomBook
        };

        await userRoomBookRepo.AddRangeAsync(new[] { userRoomBook }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(penaltySettings),
            notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Штраф: 150 руб."));

            var updatedUrb = userRoomBookRepo.Entities.First();
            Assert.That(updatedUrb.Penalty, Is.EqualTo(150m)); // проверяем применился ли MaxPenalty
        });
    }

    [Test]
    public async Task Execute_WhenBookHasExistingPenaltyFromDeadlineCheck_TakesMax()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var penaltySettings = new PenaltySettings { DailyRate = 10m, GracePeriodDays = 0 };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 1,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Overdue Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        var user = new User { Id = new Id(Guid.NewGuid()), Email = "user@test.com" };

        // Просрочка в 3 дня. Текущий расчет даст штраф 30 руб.
        var deadline = DateTime.Now.AddDays(-3);

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = deadline.AddDays(-14),
            Deadline = deadline,
            ReturnDate = null,
            Penalty = 50m, // DeadlineCheckService когда-то уже насчитала 50 руб
            User = user,
            RoomBook = roomBook
        };

        await userRoomBookRepo.AddRangeAsync(new[] { userRoomBook }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(penaltySettings),
            notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedUrb = userRoomBookRepo.Entities.First();
            // Сравнивает Math.Max(30 текущих, 50 сохраненных), должно остаться 50
            Assert.That(updatedUrb.Penalty, Is.EqualTo(50m));
        });
    }
    [Test]
    public async Task Execute_WhenBookHasExistingPenalty_LowerThanCalculated_TakesCalculated()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var penaltySettings = new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = null,
            GracePeriodDays = 0
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 1,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Overdue Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        var user = new User { Id = new Id(Guid.NewGuid()), Email = "user@test.com" };

        // Deadline в прошлом на 8 дней
        var deadline = DateTime.Now.AddDays(-8);

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = deadline.AddDays(-14),
            Deadline = deadline,
            ReturnDate = null,
            Penalty = 50m, // DeadlineCheckService насчитал 50 руб
            User = user,
            RoomBook = roomBook
        };

        await userRoomBookRepo.AddRangeAsync(new[] { userRoomBook }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(penaltySettings),
            notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Штраф"));

            var updatedUrb = userRoomBookRepo.Entities.First();
            // Проверяем что штраф больше 50 (существующего) и кратен 10
            Assert.That(updatedUrb.Penalty, Is.GreaterThan(50m));
            Assert.That(updatedUrb.Penalty % 10, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Execute_WhenBookHasExistingPenalty_HigherThanCalculated_TakesExisting()
    {
        // Arrange
        var unitOfWork2 = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork2.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork2.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var penaltySettings = new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = null,
            GracePeriodDays = 0
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 1,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Overdue Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        var user = new User { Id = new Id(Guid.NewGuid()), Email = "user@test.com" };

        // Deadline в прошлом на 1 день (штраф = 10)
        var deadline = DateTime.Now.AddDays(-1);

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = deadline.AddDays(-14),
            Deadline = deadline,
            ReturnDate = null,
            Penalty = 50m, // Существующий штраф
            User = user,
            RoomBook = roomBook
        };

        await userRoomBookRepo.AddRangeAsync([userRoomBook]);
        await roomBookRepo.AddRangeAsync([roomBook]);

        var command = new ReturnBookCommand(
            unitOfWork2,
            CreateValidator(),
            CreatePenaltyService(penaltySettings),
            notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedUrb = userRoomBookRepo.Entities.First();
            // Существующий штраф 50 > рассчитанного ~10 → остаётся 50
            Assert.That(updatedUrb.Penalty, Is.EqualTo(50m));

            Assert.That(response.Message, Does.Contain("Штраф: 50"));
        });
    }

    [Test]
    public async Task Execute_WhenExtraDaysIsZero_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork3 = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork3.GetRepository<UserRoomBook>();
        var settings = new BorrowingSettings { MaxExtendDeadlineDays = 14 };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = new DateTime(2024, 1, 1, 10, 0, 0),
            Deadline = new DateTime(2024, 1, 15, 10, 0, 0),
            ReturnDate = null
        };
        await userRoomBookRepo.AddRangeAsync([userRoomBook]);

        var command = new ExtendDeadlineCommand(unitOfWork3, CreateValidator(settings));
        var request = new ExtendDeadlineRequest { UserRoomBookId = userRoomBook.Id.Value, ExtraDays = 0 };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));

        Assert.That(ex.ExceptionDetails, Contains.Item("Срок продления должен быть от 1 до 14 дней"));
    }
    [Test]
    public async Task Execute_WhenReturnedOnTime_MessageDoesNotContainPenalty()
    {
        // Arrange
        var unitOfWork4 = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork4.GetRepository<UserRoomBook>();
        var roomBookRepo = (FakeRepository<RoomBook>)unitOfWork4.GetRepository<RoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 2,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "On Time Book" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        var user = new User { Id = new Id(Guid.NewGuid()), Email = "user@test.com" };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = DateTime.Now.AddDays(-10),
            Deadline = DateTime.Now.AddDays(5), // Ещё не просрочена
            ReturnDate = null,
            Penalty = null,
            User = user,
            RoomBook = roomBook
        };

        await userRoomBookRepo.AddRangeAsync([userRoomBook]);
        await roomBookRepo.AddRangeAsync([roomBook]);

        var command = new ReturnBookCommand(
            unitOfWork4,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object);

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Книга возвращена"));

            var updatedUrb = userRoomBookRepo.Entities.First();
            Assert.That(updatedUrb.ReturnDate, Is.Not.Null);
            Assert.That(updatedUrb.IsReturned, Is.True);
            Assert.That(roomBook.BorrowedCount, Is.EqualTo(1));

            notificationMock.Verify(
                x => x.SendOverdueNotificationAsync(
                    It.IsAny<User>(), It.IsAny<UserRoomBook>(),
                    It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
                Times.Never);
        });
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

        var validator = new BorrowingValidatorAsync(Mock.Of<IOptionsSnapshot<BorrowingSettings>>());
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
