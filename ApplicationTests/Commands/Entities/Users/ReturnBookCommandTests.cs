namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class ReturnBookCommandTests
{
    private static PenaltyCalculatorService CreatePenaltyService(PenaltySettings? settings = null)
    {
        var optionsMock = new Mock<IOptionsSnapshot<PenaltySettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings ?? new PenaltySettings());
        return new PenaltyCalculatorService(optionsMock.Object);
    }

    private static BorrowingValidatorAsync CreateValidator(BorrowingSettings? settings = null)
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
            notificationMock.Object,
            new FakeCurrentUserService());

        var request = new ReturnBookRequest { UserRoomBookId = Guid.NewGuid() };

        // Act & Assert
        await Assert.ThrowsAsync<UserRoomBookNotFoundException>(() =>
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
        await userRoomBookRepo.AddRangeAsync([userRoomBook], CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object,
            new FakeCurrentUserService());

        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act & Assert
        await Assert.ThrowsAsync<LibValidationException>(() =>
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

        await userRoomBookRepo.AddRangeAsync([userRoomBook], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object,
            new FakeCurrentUserService());

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

        await userRoomBookRepo.AddRangeAsync([userRoomBook], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object,
            new FakeCurrentUserService());

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

        await userRoomBookRepo.AddRangeAsync([userRoomBook], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object,
            new FakeCurrentUserService());

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

        await userRoomBookRepo.AddRangeAsync([userRoomBook], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object,
            new FakeCurrentUserService());

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
            Penalty = 50m,        // DeadlineCheckService когда-то уже насчитала 50 руб
            User = user,
            RoomBook = roomBook
        };

        await userRoomBookRepo.AddRangeAsync([userRoomBook], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object,
            new FakeCurrentUserService());

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

        await userRoomBookRepo.AddRangeAsync([userRoomBook], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);

        var command = new ReturnBookCommand(
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object,
            new FakeCurrentUserService());

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
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object,
            new FakeCurrentUserService());

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
        var unitOfWork = new FakeUnitOfWork();
        var userRoomBookRepo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var settings = new BorrowingSettings { MaxExtendDeadlineDays = 14 };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = new DateTime(2024, 1, 1, 10, 0, 0),
            Deadline = new DateTime(2024, 1, 15, 10, 0, 0),
            ReturnDate = null
        };
        await userRoomBookRepo.AddRangeAsync([userRoomBook]);

        var command = new ExtendDeadlineCommand(unitOfWork, CreateValidator(settings));
        var request = new ExtendDeadlineRequest { UserRoomBookId = userRoomBook.Id.Value, ExtraDays = 0 };

        // Act & Assert
        await Assert.MultipleAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
                command.Execute(request, CancellationToken.None));
            Assert.That(ex.ExceptionDetails, Contains.Item("Срок продления должен быть больше 0"));
        });
    }

    [Test]
    public async Task Execute_WhenReturnedOnTime_MessageDoesNotContainPenalty()
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
            unitOfWork,
            CreateValidator(),
            CreatePenaltyService(),
            notificationMock.Object,
            new FakeCurrentUserService());

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
        var command = new ReturnBookCommand(
            unitOfWorkMock.Object, validator, penaltyService, notificationMock.Object,
            new FakeCurrentUserService());
        var request = new ReturnBookRequest { UserRoomBookId = userRoomBook.Id.Value };

        // Act & Assert
        await Assert.MultipleAsync(async () =>
        {
            await Assert.ThrowsAsync<Exception>(() => command.Execute(request, CancellationToken.None));

            unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        });
    }

    #region Discard on return

    // Общий хелпер: создаёт минимальный граф (Book → RoomBook → UserRoomBook) и репозитории.
    // Возвращает всё нужное, чтобы каждый тест был самостоятельным и без «... (аналогично)».
    private static (
        FakeUnitOfWork uow,
        FakeRepository<UserRoomBook> urbRepo,
        FakeRepository<RoomBook> rbRepo,
        FakeRepository<Book> bookRepo,
        FakeRepository<DiscardedBook> discardedRepo,
        Book book,
        Room room,
        RoomBook roomBook,
        UserRoomBook userRoomBook,
        User user)
        CreateReturnScenario(int bookCount = 5, int borrowedCount = 1)
    {
        var uow = new FakeUnitOfWork();
        var urbRepo = (FakeRepository<UserRoomBook>)uow.GetRepository<UserRoomBook>();
        var rbRepo = (FakeRepository<RoomBook>)uow.GetRepository<RoomBook>();
        var bookRepo = (FakeRepository<Book>)uow.GetRepository<Book>();
        var discardedRepo = (FakeRepository<DiscardedBook>)uow.GetRepository<DiscardedBook>();

        var book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book", IsDiscarded = false };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" };
        var user = new User { Id = new Id(Guid.NewGuid()), Email = "user@test.com", LastName = "Test", FirstName = "User" };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = room,
            BookCount = bookCount,
            BorrowedCount = borrowedCount
        };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            RoomBook = roomBook,
            BorrowDate = DateTime.UtcNow.AddDays(-3),
            Deadline = DateTime.UtcNow.AddDays(5),
            ReturnDate = null
        };

        book.RoomBook.Add(roomBook);
        user.RoomBooks.Add(userRoomBook);

        bookRepo.AddRangeAsync([book], CancellationToken.None).GetAwaiter().GetResult();
        rbRepo.AddRangeAsync([roomBook], CancellationToken.None).GetAwaiter().GetResult();
        urbRepo.AddRangeAsync([userRoomBook], CancellationToken.None).GetAwaiter().GetResult();

        return (uow, urbRepo, rbRepo, bookRepo, discardedRepo, book, room, roomBook, userRoomBook, user);
    }

    [Test]
    public async Task Execute_WithDiscardReasonWear_CreatesDiscardedBookWithoutCompensation()
    {
        // Arrange
        var (uow, _, _, _, discardedRepo, book, _, roomBook, userRoomBook, _) = CreateReturnScenario();
        var command = new ReturnBookCommand(
            uow, CreateValidator(), CreatePenaltyService(),
            new Mock<INotificationService>().Object, new FakeCurrentUserService());

        var request = new ReturnBookRequest
        {
            UserRoomBookId = userRoomBook.Id.Value,
            DiscardReason = DiscardReason.Wear,
            CompensationAmount = null
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Списана (износ)"));

            Assert.That(roomBook.BookCount, Is.EqualTo(4));         // 5 => 4
            Assert.That(roomBook.BorrowedCount, Is.EqualTo(0));     // 1 => 0
            Assert.That(book.IsDiscarded, Is.False);                // 4 > 0, не списана

            Assert.That(discardedRepo.Entities, Has.Count.EqualTo(1));
            var d = discardedRepo.Entities.First();
            Assert.That(d.Amount, Is.EqualTo(1));
            Assert.That(d.DiscardReason, Is.EqualTo(DiscardReason.Wear));
            Assert.That(d.CompensationAmount, Is.Null);
            Assert.That(d.ApprovedById, Is.Not.Null);
            Assert.That(d.ApprovedBy, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task Execute_WithDiscardReasonLoss_WithCompensation_CreatesDiscardedBook()
    {
        // Arrange
        var (uow, _, _, _, discardedRepo, _, _, roomBook, userRoomBook, _) = CreateReturnScenario();
        var command = new ReturnBookCommand(
            uow, CreateValidator(), CreatePenaltyService(),
            new Mock<INotificationService>().Object, new FakeCurrentUserService());

        var request = new ReturnBookRequest
        {
            UserRoomBookId = userRoomBook.Id.Value,
            DiscardReason = DiscardReason.Loss,
            CompensationAmount = 500m
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Списана (утрата)"));
            Assert.That(response.Message, Does.Contain("Компенсация: 500"));

            Assert.That(roomBook.BookCount, Is.EqualTo(4));
            Assert.That(roomBook.BorrowedCount, Is.EqualTo(0));

            Assert.That(discardedRepo.Entities, Has.Count.EqualTo(1));
            var d = discardedRepo.Entities.First();
            Assert.That(d.DiscardReason, Is.EqualTo(DiscardReason.Loss));
            Assert.That(d.CompensationAmount, Is.EqualTo(500m));
        });
    }

    [Test]
    public async Task Execute_WithDiscardReasonLoss_WithoutCompensation_Throws()
    {
        // Arrange
        var (uow, _, _, _, _, _, _, _, userRoomBook, _) = CreateReturnScenario();
        var command = new ReturnBookCommand(
            uow, CreateValidator(), CreatePenaltyService(),
            new Mock<INotificationService>().Object, new FakeCurrentUserService());

        var request = new ReturnBookRequest
        {
            UserRoomBookId = userRoomBook.Id.Value,
            DiscardReason = DiscardReason.Loss,
            CompensationAmount = null
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails,
            Has.Some.Contains("При утрате книги необходимо указать сумму компенсации"));
    }

    [Test]
    public async Task Execute_WithDiscardReasonLastCopy_SetsBookIsDiscarded()
    {
        // Arrange: BookCount = 1, BorrowedCount = 1 → после списания 0
        var (uow, _, _, _, _, book, _, roomBook, userRoomBook, _) =
            CreateReturnScenario(bookCount: 1, borrowedCount: 1);
        var command = new ReturnBookCommand(
            uow, CreateValidator(), CreatePenaltyService(),
            new Mock<INotificationService>().Object, new FakeCurrentUserService());

        var request = new ReturnBookRequest
        {
            UserRoomBookId = userRoomBook.Id.Value,
            DiscardReason = DiscardReason.Wear
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(roomBook.BookCount, Is.EqualTo(0));
            Assert.That(roomBook.BorrowedCount, Is.EqualTo(0));
            Assert.That(book.IsDiscarded, Is.True);
        });
    }

    [Test]
    public async Task Execute_WithNullDiscardReason_DoesNotCreateDiscardedBook()
    {
        // Arrange
        var (uow, _, _, _, discardedRepo, _, _, roomBook, userRoomBook, _) = CreateReturnScenario();
        var command = new ReturnBookCommand(
            uow, CreateValidator(), CreatePenaltyService(),
            new Mock<INotificationService>().Object, new FakeCurrentUserService());

        var request = new ReturnBookRequest
        {
            UserRoomBookId = userRoomBook.Id.Value,
            DiscardReason = null
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Книга возвращена"));
            Assert.That(discardedRepo.Entities, Is.Empty);
            Assert.That(roomBook.BookCount, Is.EqualTo(5));         // не изменилось
            Assert.That(roomBook.BorrowedCount, Is.EqualTo(0));     // 1 => 0
        });
    }

    [Test]
    public async Task Execute_WithNegativeCompensation_Throws()
    {
        // Arrange
        var (uow, _, _, _, _, _, _, _, userRoomBook, _) = CreateReturnScenario();
        var command = new ReturnBookCommand(
            uow, CreateValidator(), CreatePenaltyService(),
            new Mock<INotificationService>().Object, new FakeCurrentUserService());

        var request = new ReturnBookRequest
        {
            UserRoomBookId = userRoomBook.Id.Value,
            DiscardReason = DiscardReason.Damaged,
            CompensationAmount = -10m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails,
            Has.Some.Contains("Сумма компенсации не может быть отрицательной"));
    }

    [Test]
    public async Task Execute_WithDiscardAndPenalty_MessageContainsBoth()
    {
        // Arrange: Deadline в прошлом → штраф начислится
        var (uow, _, _, _, _, _, _, roomBook, userRoomBook, _) =
            CreateReturnScenario(bookCount: 1, borrowedCount: 1);
        userRoomBook.Deadline = DateTime.UtcNow.AddDays(-5);   // просрочка 5 дней

        var command = new ReturnBookCommand(
            uow, CreateValidator(), CreatePenaltyService(),
            new Mock<INotificationService>().Object, new FakeCurrentUserService());

        var request = new ReturnBookRequest
        {
            UserRoomBookId = userRoomBook.Id.Value,
            DiscardReason = DiscardReason.Loss,
            CompensationAmount = 300m
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Штраф"));
            Assert.That(response.Message, Does.Contain("Списана (утрата)"));
            Assert.That(response.Message, Does.Contain("Компенсация: 300"));
            Assert.That(userRoomBook.Penalty, Is.GreaterThan(0));
            Assert.That(roomBook.BookCount, Is.EqualTo(0));
        });
    }

    #endregion


}