namespace LibApp.ApplicationTests.Services;

[TestFixture]
public class ExpireBorrowRequestsServiceTests
{
    /// <summary>
    /// Наследник, выставляющий <c>ExpireAsync</c> для прямого вызова.
    /// BackgroundService.ExecuteAsync содержит бесконечный цикл с Task.Delay —
    /// в тестах его не запускаем, проверяем один цикл в изоляции.
    /// </summary>
    private sealed class TestableExpireService : ExpireBorrowRequestsService
    {
        public TestableExpireService(
            IServiceProvider serviceProvider,
            ILogger<ExpireBorrowRequestsService> logger,
            IOptionsMonitor<BorrowingSettings> settingsMonitor)
            : base(serviceProvider, logger, settingsMonitor)
        {
        }

        public Task<int> RunExpireAsync(CancellationToken ct) => ExpireAsync(ct);
    }

    private static (TestableExpireService Service,
                    Mock<IRepository<BorrowRequest>> RequestRepo,
                    Mock<IAuditService> Audit,
                    Mock<IUnitOfWork> UnitOfWork)
        CreateService(IEnumerable<BorrowRequest>? approvedRequests = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var requestRepo = new Mock<IRepository<BorrowRequest>>();

        unitOfWork.Setup(u => u.GetRepository<BorrowRequest>()).Returns(requestRepo.Object);

        unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, ct) =>
            {
                await unitOfWork.Object.BeginTransactionAsync(ct);
                try
                {
                    await action();
                    await unitOfWork.Object.SaveChangesAsync(ct);
                    await unitOfWork.Object.CommitTransactionAsync(ct);
                }
                catch
                {
                    await unitOfWork.Object.RollbackTransactionAsync(ct);
                    throw;
                }
            });
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // GetAsync с фильтром: возвращаем то, что тест передал в approvedRequests.
        // Фильтр не вычисляем — это ответственность EF Core, в юнит-тесте проверяем
        // последующую обработку: смену статуса, декремент, батч-аудит.
        requestRepo
            .Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>?>(),
                null,
                null,
                null,
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(approvedRequests ?? []);

        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.LogAsync(
                It.IsAny<AuditActionType>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<Id?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped(_ => unitOfWork.Object);
        services.AddScoped(_ => audit.Object);
        services.AddSingleton(TimeProvider.System);
        var sp = services.BuildServiceProvider();

        var settingsMonitor = new Mock<IOptionsMonitor<BorrowingSettings>>();
        settingsMonitor.SetupGet(m => m.CurrentValue).Returns(new BorrowingSettings());

        var logger = new Mock<ILogger<ExpireBorrowRequestsService>>();

        var service = new TestableExpireService(sp, logger.Object, settingsMonitor.Object);
        return (service, requestRepo, audit, unitOfWork);
    }

    private static BorrowRequest NewApprovedRequest(DateTime expiresAt, int reservedCount = 1)
    {
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 1,
            ReservedCount = reservedCount,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book", Author = "Test Author" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        return new BorrowRequest
        {
            Id = new Id(Guid.NewGuid()),
            UserId = new Id(Guid.NewGuid()),
            RoomBookId = roomBook.Id,
            RoomBook = roomBook,
            Status = BorrowRequestStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddHours(-72),
            ProcessedAt = DateTime.UtcNow.AddHours(-72),
            ProcessedByUserId = new Id(Guid.NewGuid()),
            ExpiresAt = expiresAt
        };
    }

    [Test]
    public async Task ExpireAsync_WhenNoExpiredRequests_DoesNotWriteAudit()
    {
        // Arrange: репозиторий вернул пустой список (нет истёкших).
        var (service, _, audit, unitOfWork) = CreateService(approvedRequests: []);

        // Act
        var count = await service.RunExpireAsync(CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(count, Is.EqualTo(0));
            audit.Verify(a => a.LogAsync(
                It.IsAny<AuditActionType>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<Id?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Test]
    public async Task ExpireAsync_WhenSingleExpired_MarksExpiredAndDecrementsReserved()
    {
        // Arrange: бронь истекла час назад, ReservedCount = 1.
        var expired = NewApprovedRequest(DateTime.UtcNow.AddHours(-1), reservedCount: 1);
        var (service, _, audit, _) = CreateService(approvedRequests: [expired]);

        // Act
        var count = await service.RunExpireAsync(CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(expired.Status, Is.EqualTo(BorrowRequestStatus.Expired));
            Assert.That(expired.RoomBook.ReservedCount, Is.EqualTo(0));

            audit.Verify(a => a.LogAsync(
                AuditActionType.Update,
                nameof(BorrowRequest),
                null,
                It.Is<string?>(details => details != null && details.Contains("Истекло броней: 1")),
                null,
                "Система: истечение броней",
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Test]
    public async Task ExpireAsync_WhenMultipleExpired_WritesSingleBatchAudit()
    {
        // Arrange: три истёкшие брони в разных комнатах.
        var a = NewApprovedRequest(DateTime.UtcNow.AddHours(-1), reservedCount: 2);
        var b = NewApprovedRequest(DateTime.UtcNow.AddHours(-2), reservedCount: 1);
        var c = NewApprovedRequest(DateTime.UtcNow.AddHours(-3), reservedCount: 1);

        var (service, _, audit, _) = CreateService(approvedRequests: [a, b, c]);

        // Act
        var count = await service.RunExpireAsync(CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(count, Is.EqualTo(3));

            Assert.That(a.Status, Is.EqualTo(BorrowRequestStatus.Expired));
            Assert.That(b.Status, Is.EqualTo(BorrowRequestStatus.Expired));
            Assert.That(c.Status, Is.EqualTo(BorrowRequestStatus.Expired));

            Assert.That(a.RoomBook.ReservedCount, Is.EqualTo(1));
            Assert.That(b.RoomBook.ReservedCount, Is.EqualTo(0));
            Assert.That(c.RoomBook.ReservedCount, Is.EqualTo(0));

            // Одна запись за цикл, не три.
            audit.Verify(a => a.LogAsync(
                AuditActionType.Update,
                nameof(BorrowRequest),
                null,
                It.Is<string?>(details => details != null && details.Contains("Истекло броней: 3")),
                null,
                "Система: истечение броней",
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Test]
    public async Task ExpireAsync_WhenExpiresAtNull_IgnoresRequest()
    {
        // Arrange: Approved без ExpiresAt. В EF-запросе такие строки отсекаются
        // фильтром, но defensive-проверка в цикле защищает от регрессий.
        var anomalous = NewApprovedRequest(expiresAt: default, reservedCount: 1);
        anomalous.ExpiresAt = null;

        var (service, _, audit, _) = CreateService(approvedRequests: [anomalous]);

        // Act
        var count = await service.RunExpireAsync(CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(anomalous.Status, Is.EqualTo(BorrowRequestStatus.Approved));
            Assert.That(anomalous.RoomBook.ReservedCount, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(0));

            audit.Verify(a => a.LogAsync(
                It.IsAny<AuditActionType>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<Id?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}