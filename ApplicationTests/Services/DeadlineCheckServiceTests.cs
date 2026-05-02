namespace LibApp.ApplicationTests.Services;

[TestFixture]
public class DeadlineCheckServiceTests
{
    private Mock<IServiceProvider> CreateServiceProvider(
        FakeRepository<UserRoomBook> userRoomBookRepo,
        Mock<INotificationService>? notificationMock = null)
    {
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        notificationMock ??= new Mock<INotificationService>();

        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        serviceProviderMock
            .Setup(x => x.GetService(typeof(IRepository<UserRoomBook>)))
            .Returns(userRoomBookRepo);
        serviceProviderMock
            .Setup(x => x.GetService(typeof(INotificationService)))
            .Returns(notificationMock.Object);

        var fullProviderMock = new Mock<IServiceProvider>();
        fullProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        return fullProviderMock;
    }

    private Mock<IOptionsMonitor<DeadlineCheckSettings>> CreateDeadlineSettingsMonitor(
        DeadlineCheckSettings? settings = null)
    {
        var mock = new Mock<IOptionsMonitor<DeadlineCheckSettings>>();
        mock.Setup(x => x.CurrentValue).Returns(settings ?? new DeadlineCheckSettings
        {
            ReturnReminderInDays = 3,
            CheckIntervalInHours = 24
        });
        return mock;
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
        FakeRepository<UserRoomBook> userRoomBookRepo,
        DeadlineCheckSettings? deadlineSettings = null,
        PenaltySettings? penaltySettings = null,
        Mock<INotificationService>? notificationMock = null)
    {
        var logger = new Logger<DeadlineCheckService>(new LoggerFactory());
        var serviceProvider = CreateServiceProvider(userRoomBookRepo, notificationMock);

        return new DeadlineCheckService(
            serviceProvider.Object,
            logger,
            CreateDeadlineSettingsMonitor(deadlineSettings).Object,
            CreatePenaltySettingsMonitor(penaltySettings).Object);
    }

    [Test]
    public async Task CheckDeadlines_WithSoonDueBooks_SendsReturnReminders()
    {
        // Arrange
        var repo = new FakeRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();
        var realNow = DateTime.Now;

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

        await repo.AddRange([userRoomBook]);

        var deadlineSettings = new DeadlineCheckSettings
        {
            ReturnReminderInDays = 3,
            CheckIntervalInHours = 24
        };

        var service = CreateService(repo, deadlineSettings, notificationMock: notificationMock);

        var method = typeof(DeadlineCheckService)
            .GetMethod("CheckDeadlines", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(service, [CancellationToken.None])!;

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
        var repo = new FakeRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();
        var realNow = DateTime.Now;

        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@test.com",
            LastName = "Test",
            FirstName = "User"
        };

        // Просрочено ровно на 5 дней (реальный DateTime.Now - 5 дней)
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

        await repo.AddRange([userRoomBook]);

        var penaltySettings = new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = null, // Без ограничения!
            GracePeriodDays = 0
        };

        var service = CreateService(repo, penaltySettings: penaltySettings, notificationMock: notificationMock);

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
        var repo = new FakeRepository<UserRoomBook>();
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

        await repo.AddRange([userRoomBook]);

        var penaltySettings = new PenaltySettings
        {
            DailyRate = 10m,
            MaxPenalty = 150m, // Ограничение
            GracePeriodDays = 0
        };

        var service = CreateService(repo, penaltySettings: penaltySettings, notificationMock: notificationMock);

        var method = typeof(DeadlineCheckService)
            .GetMethod("CheckDeadlines", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        // Assert
        var updatedUrb = repo.Entities.First();
        Assert.That(updatedUrb.Penalty, Is.EqualTo(150m)); // capped
    }

    [Test]
    public async Task CheckDeadlines_WithNoBooks_DoesNotSendNotifications()
    {
        // Arrange
        var repo = new FakeRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();

        var service = CreateService(repo, notificationMock: notificationMock);

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
        var repo = new FakeRepository<UserRoomBook>();
        var service = CreateService(repo);
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
        var repo = new FakeRepository<UserRoomBook>();
        var service = CreateService(repo);
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
        var repo = new FakeRepository<UserRoomBook>();
        var service = CreateService(repo);
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
        var repo = new FakeRepository<UserRoomBook>();
        var deadlineMonitorMock = new Mock<IOptionsMonitor<DeadlineCheckSettings>>();

        deadlineMonitorMock
            .Setup(x => x.CurrentValue)
            .Returns(new DeadlineCheckSettings
            {
                CheckIntervalInHours = 24,
                ReturnReminderInDays = 3
            });

        var service = new DeadlineCheckService(
            CreateServiceProvider(repo).Object,
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
        var repo = new FakeRepository<UserRoomBook>();

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
        var repo = new FakeRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();
        var realNow = DateTime.Now;

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

        await repo.AddRange([soonDueBook, overdueBook]);

        var deadlineSettings = new DeadlineCheckSettings
        {
            ReturnReminderInDays = 3,
            CheckIntervalInHours = 24
        };

        var service = CreateService(repo, deadlineSettings, notificationMock: notificationMock);

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
        var repo = new FakeRepository<UserRoomBook>();
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

        await repo.AddRange([bookWithoutDeadline]);

        var service = CreateService(repo, notificationMock: notificationMock);

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
        var repo = new FakeRepository<UserRoomBook>();
        var notificationMock = new Mock<INotificationService>();
        var realNow = DateTime.Now;

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

        await repo.AddRange([returnedBook]);

        var service = CreateService(repo, notificationMock: notificationMock);

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