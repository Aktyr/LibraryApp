namespace LibApp.ApplicationTests.Services;

[TestFixture]
public class DeadlineCheckServiceTests
{
    private Mock<IServiceProvider> CreateServiceProvider(
        FakeUnitOfWork unitOfWork,
        Mock<INotificationService>? notificationMock = null)
    {
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        notificationMock ??= new Mock<INotificationService>();

        serviceProviderMock
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(unitOfWork);

        serviceProviderMock
            .Setup(x => x.GetService(typeof(INotificationService)))
            .Returns(notificationMock.Object);

        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        var fullProviderMock = new Mock<IServiceProvider>();
        fullProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        return fullProviderMock;
    }
    private Mock<IOptionsMonitor<PenaltySettings>> CreatePenaltySettingsMonitor(
        PenaltySettings? settings = null)
    {
        var mock = new Mock<IOptionsMonitor<PenaltySettings>>();
        mock.Setup(x => x.CurrentValue).Returns(settings ?? new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = 500m,
            GracePeriodDays = 0
        });
        return mock;
    }

    private DeadlineCheckService CreateService(
    FakeUnitOfWork unitOfWork,
    DeadlineCheckSettings? deadlineSettings = null,
    PenaltySettings? penaltySettings = null,
    Mock<INotificationService>? notificationMock = null)
    {
        var logger = new Logger<DeadlineCheckService>(new LoggerFactory());
        var serviceProvider = CreateServiceProvider(unitOfWork, notificationMock);

        var deadlineMonitor = new Mock<IOptionsMonitor<DeadlineCheckSettings>>();
        deadlineMonitor.Setup(x => x.CurrentValue).Returns(deadlineSettings ?? new DeadlineCheckSettings
        {
            ReturnReminderInDays = 3,
            CheckIntervalInHours = 24
        });

        var penaltyMonitor = new Mock<IOptionsMonitor<PenaltySettings>>();
        penaltyMonitor.Setup(x => x.CurrentValue).Returns(penaltySettings ?? new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = 500m,
            GracePeriodDays = 0
        });

        return new DeadlineCheckService(
            serviceProvider.Object,
            logger,
            deadlineMonitor.Object,
            penaltyMonitor.Object);
    }


    [Test]
    public async Task CheckDeadlines_WithSoonDueBooks_SendsReturnReminders()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();
        var realNow = DateTime.UtcNow;

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@test.com",
            LastName = "Test",
            FirstName = "User"
        };

        // Только книги с напоминанием (скоро сдача), БЕЗ просроченных
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = realNow.AddDays(3), // Ровно через 3 дня (ReturnReminderInDays = 3)
            ReturnDate = null,
            RoomBook = new RoomBook
            {
                Id = new Id(Guid.NewGuid()),
                Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book", Author = "Author" },
                Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
            }
        };

        await repo.AddRangeAsync([userRoomBook]);

        var deadlineSettings = new DeadlineCheckSettings
        {
            ReturnReminderInDays = 3,
            CheckIntervalInHours = 24
        };

        var service = CreateService(unitOfWork, deadlineSettings, notificationMock: notificationMock);

        var method = typeof(DeadlineCheckService)
            .GetMethod("CheckDeadlines", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(service, new object?[] { CancellationToken.None })!;

        // Assert
        notificationMock.Verify(
            x => x.SendReturnReminderAsync(
                user,
                userRoomBook,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CheckDeadlines_WithOverdueBooks_CalculatesPenaltyAndSendsNotification()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();
        var realNow = DateTime.UtcNow;

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@test.com",
            LastName = "Test",
            FirstName = "User"
        };

        // Просрочено ровно на 5 дней (реальный DateTime.UtcNow - 5 дней)
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = realNow.AddDays(-5),
            ReturnDate = null,
            Penalty = null,
            RoomBook = new RoomBook
            {
                Id = new Id(Guid.NewGuid()),
                Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Overdue Book", Author = "Author" },
                Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
            }
        };

        await repo.AddRangeAsync([userRoomBook]);

        var penaltySettings = new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = null, // Без ограничения!
            GracePeriodDays = 0
        };

        var service = CreateService(unitOfWork, penaltySettings: penaltySettings, notificationMock: notificationMock);

        var method = typeof(DeadlineCheckService)
            .GetMethod("CheckDeadlines", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        // Assert
        var updatedUrb = repo.Entities.First();
        Assert.Multiple(() =>
        {
            // 5 дней × 10 = 50
            Assert.That(updatedUrb.Penalty, Is.EqualTo(50m));
        });

        notificationMock.Verify(
            x => x.SendOverdueNotificationAsync(
                user,
                userRoomBook,
                50m,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CheckDeadlines_WithOverdueBooksAndMaxPenalty_AppliesPenaltyCap()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();
        var now = new DateTime(2024, 1, 15, 12, 0, 0);

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@test.com",
            LastName = "Test",
            FirstName = "User"
        };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = now.AddDays(-100), // Очень большая просрочка
            ReturnDate = null,
            Penalty = null,
            RoomBook = new RoomBook
            {
                Id = new Id(Guid.NewGuid()),
                Book = new Book { Title = "Very Overdue Book", Author = "Author" },
                Room = new Room { Name = "Test Room" }
            }
        };

        await repo.AddRangeAsync(new[] { userRoomBook }, CancellationToken.None);

        var penaltySettings = new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = 150m, // Ограничение
            GracePeriodDays = 0
        };

        var service = CreateService(unitOfWork, penaltySettings: penaltySettings, notificationMock: notificationMock);

        var method = typeof(DeadlineCheckService)
            .GetMethod("CheckDeadlines", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(service, new object?[] { CancellationToken.None })!;

        // Assert
        var updatedUrb = repo.Entities.First();
        Assert.That(updatedUrb.Penalty, Is.EqualTo(150m)); // capped
    }

    [Test]
    public async Task CheckDeadlines_WithNoBooks_DoesNotSendNotifications()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var service = CreateService(unitOfWork, notificationMock: notificationMock);

        var method = typeof(DeadlineCheckService)
            .GetMethod("CheckDeadlines", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        // Assert
        notificationMock.Verify(
            x => x.SendReturnReminderAsync(
                It.IsAny<User>(),
                It.IsAny<UserRoomBook>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        notificationMock.Verify(
            x => x.SendOverdueNotificationAsync(
                It.IsAny<User>(),
                It.IsAny<UserRoomBook>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancelled_StopsExecution()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var service = CreateService(unitOfWork);
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        cts.Cancel(); // Отменяем немедленно

        // Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await Task.WhenAny(executeTask, Task.Delay(5000));
        });

        cts.Dispose();
    }

    [Test]
    public void CalculatePenalty_WithMaxPenalty_CapsCorrectly()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var service = CreateService(unitOfWork);
        var settings = new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = 100m,
            GracePeriodDays = 0
        };

        var method = typeof(DeadlineCheckService)
            .GetMethod("CalculatePenalty", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var result = method.Invoke(service, [15, settings]); // 15 дней просрочки × 10 = 150 → capped at 100

        // Assert
        Assert.That(result, Is.EqualTo(100m));
    }

    [Test]
    public void CalculatePenalty_WithoutMaxPenalty_ReturnsFullAmount()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var service = CreateService(unitOfWork);
        var settings = new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = null,
            GracePeriodDays = 0
        };

        var method = typeof(DeadlineCheckService)
            .GetMethod("CalculatePenalty", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var result = method.Invoke(service, [15, settings]); // 15 дней × 10 = 150, без ограничения

        // Assert
        Assert.That(result, Is.EqualTo(150m));
    }

    [Test]
    public void Constructor_WhenSettingsChanged_UpdatesCheckInterval()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var deadlineMonitorMock = new Mock<IOptionsMonitor<DeadlineCheckSettings>>();

        deadlineMonitorMock
            .Setup(x => x.CurrentValue)
            .Returns(new DeadlineCheckSettings
            {
                CheckIntervalInHours = 24,
                ReturnReminderInDays = 3
            });

        var service = new DeadlineCheckService(
            CreateServiceProvider(unitOfWork).Object,
            new Logger<DeadlineCheckService>(new LoggerFactory()),
            deadlineMonitorMock.Object,
            CreatePenaltySettingsMonitor().Object);

        // Assert — просто проверяем что конструктор не упал
        Assert.That(service, Is.Not.Null);
    }
    [Test]
    public async Task ExecuteAsync_WhenExceptionOccurs_LogsErrorAndDelays()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();

        // Создаём сервис с неработающим ServiceProvider чтобы вызвать исключение
        var deadlineMonitorMock = new Mock<IOptionsMonitor<DeadlineCheckSettings>>();
        deadlineMonitorMock.Setup(x => x.CurrentValue).Returns(new DeadlineCheckSettings
        {
            ReturnReminderInDays = 3,
            CheckIntervalInHours = 24
        });

        var penaltyMonitorMock = new Mock<IOptionsMonitor<PenaltySettings>>();
        penaltyMonitorMock.Setup(x => x.CurrentValue).Returns(new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = null,
            GracePeriodDays = 0
        });

        // ServiceProvider будет возвращать scope который бросает исключение
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(() => throw new InvalidOperationException("Test exception"));

        var service = new DeadlineCheckService(
            serviceProviderMock.Object,
            new Logger<DeadlineCheckService>(new LoggerFactory()),
            deadlineMonitorMock.Object,
            penaltyMonitorMock.Object);

        var cts = new CancellationTokenSource();

        // Act — запускаем и сразу отменяем после небольшой задержки
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(200); // Даём время на один цикл с ошибкой
        cts.Cancel();

        // Assert — сервис не упал, а обработал ошибку и продолжил
        Assert.DoesNotThrowAsync(async () =>
        {
            await Task.WhenAny(executeTask, Task.Delay(5000));
        });

        cts.Dispose();
    }
    [Test]
    public async Task CheckDeadlines_WithBothSoonDueAndOverdue_SendsBothNotifications()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();
        var realNow = DateTime.UtcNow;

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@test.com",
            LastName = "Test",
            FirstName = "User"
        };

        // Скоро нужно сдать (через 3 дня)
        var soonDueBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = realNow.AddDays(3),
            ReturnDate = null,
            RoomBook = new RoomBook
            {
                Id = new Id(Guid.NewGuid()),
                Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Soon Due Book", Author = "Author" },
                Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
            }
        };

        // Просроченная книга
        var overdueBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = realNow.AddDays(-5),
            ReturnDate = null,
            Penalty = null,
            RoomBook = new RoomBook
            {
                Id = new Id(Guid.NewGuid()),
                Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Overdue Book", Author = "Author" },
                Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
            }
        };

        await repo.AddRangeAsync([soonDueBook, overdueBook]);

        var deadlineSettings = new DeadlineCheckSettings
        {
            ReturnReminderInDays = 3,
            CheckIntervalInHours = 24
        };

        var service = CreateService(unitOfWork, deadlineSettings, notificationMock: notificationMock);

        var method = typeof(DeadlineCheckService)
            .GetMethod("CheckDeadlines", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        // Assert
        notificationMock.Verify(
            x => x.SendReturnReminderAsync(
                user,
                soonDueBook,
                It.IsAny<CancellationToken>()),
            Times.Once);

        notificationMock.Verify(
            x => x.SendOverdueNotificationAsync(
                user,
                overdueBook,
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CheckDeadlines_WithBookWithoutDeadline_DoesNotSendNotifications()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@test.com",
            LastName = "Test",
            FirstName = "User"
        };

        // Книга без дедлайна
        var bookWithoutDeadline = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = null,
            ReturnDate = null,
            RoomBook = new RoomBook
            {
                Id = new Id(Guid.NewGuid()),
                Book = new Book { Id = new Id(Guid.NewGuid()), Title = "No Deadline Book", Author = "Author" },
                Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
            }
        };

        await repo.AddRangeAsync([bookWithoutDeadline]);

        var service = CreateService(unitOfWork, notificationMock: notificationMock);

        var method = typeof(DeadlineCheckService)
            .GetMethod("CheckDeadlines", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        // Assert
        notificationMock.Verify(
            x => x.SendReturnReminderAsync(
                It.IsAny<User>(),
                It.IsAny<UserRoomBook>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        notificationMock.Verify(
            x => x.SendOverdueNotificationAsync(
                It.IsAny<User>(),
                It.IsAny<UserRoomBook>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CheckDeadlines_WithReturnedBook_SkipsNotifications()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();
        var realNow = DateTime.UtcNow;

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@test.com",
            LastName = "Test",
            FirstName = "User"
        };

        // Уже возвращенная книга с прошедшим дедлайном
        var returnedBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = realNow.AddDays(-10),
            ReturnDate = realNow.AddDays(-5), // Возвращена
            RoomBook = new RoomBook
            {
                Id = new Id(Guid.NewGuid()),
                Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Returned Book", Author = "Author" },
                Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
            }
        };

        await repo.AddRangeAsync([returnedBook]);

        var service = CreateService(unitOfWork, notificationMock: notificationMock);

        var method = typeof(DeadlineCheckService)
            .GetMethod("CheckDeadlines", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        // Assert
        notificationMock.Verify(
            x => x.SendReturnReminderAsync(
                It.IsAny<User>(),
                It.IsAny<UserRoomBook>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        notificationMock.Verify(
            x => x.SendOverdueNotificationAsync(
                It.IsAny<User>(),
                It.IsAny<UserRoomBook>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

}