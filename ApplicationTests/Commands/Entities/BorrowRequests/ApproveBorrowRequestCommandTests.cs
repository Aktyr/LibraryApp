namespace LibApp.ApplicationTests.Commands.Entities.BorrowRequests;

[TestFixture]
public class ApproveBorrowRequestCommandTests
{
    private const int ReserveHoldHours = 48;

    private static (ApproveBorrowRequestCommand Command,
                    Mock<IRepository<BorrowRequest>> RequestRepo,
                    Mock<INotificationService> Notification,
                    Mock<IAuditService> Audit,
                    Mock<ILogger<ApproveBorrowRequestCommand>> Logger,
                    Mock<IUnitOfWork> UnitOfWork)
        CreateCommand()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var requestRepo = new Mock<IRepository<BorrowRequest>>();

        unitOfWork.Setup(u => u.GetRepository<BorrowRequest>()).Returns(requestRepo.Object);

        // Транзакция: Begin → action → SaveChanges → Commit. Catch → Rollback → rethrow.
        // Порядок важен: в тесте на падение email Commit должен быть вызван ДО email,
        // иначе исключение email откатило бы бронь и тест не отловил бы регрессию.
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

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.LibraryUserId).Returns(new Id(Guid.NewGuid()));

        var settings = new BorrowingSettings
        {
            MaxBooksPerUser = 5,
            ReserveHoldHours = ReserveHoldHours
        };
        var settingsSnapshot = new Mock<IOptionsSnapshot<BorrowingSettings>>();
        settingsSnapshot.SetupGet(s => s.Value).Returns(settings);

        var notification = new Mock<INotificationService>();
        notification
            .Setup(n => n.SendBorrowRequestApprovedAsync(
                It.IsAny<User>(),
                It.IsAny<BorrowRequest>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<ApproveBorrowRequestCommand>>();

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

        var command = new ApproveBorrowRequestCommand(
            unitOfWork.Object,
            currentUser.Object,
            TimeProvider.System,
            settingsSnapshot.Object,
            notification.Object,
            logger.Object,
            audit.Object);

        return (command, requestRepo, notification, audit, logger, unitOfWork);
    }

    private static BorrowRequest NewPendingRequest(
        int bookCount = 5,
        int borrowedCount = 0,
        int reservedCount = 0,
        ICollection<UserRoomBook>? userActiveBorrows = null)
    {
        var userId = new Id(Guid.NewGuid());
        var user = new User
        {
            Id = userId,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            Email = "ivanov@example.com",
            SubscriptionStatus = SubscriptionStatus.Active,
            RoomBooks = userActiveBorrows ?? []
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = bookCount,
            BorrowedCount = borrowedCount,
            ReservedCount = reservedCount,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book", Author = "Test Author" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        };

        return new BorrowRequest
        {
            Id = new Id(Guid.NewGuid()),
            UserId = userId,
            User = user,
            RoomBookId = roomBook.Id,
            RoomBook = roomBook,
            Status = BorrowRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static UserRoomBook NewActiveBorrow(DateTime? deadline = null) => new()
    {
        Id = new Id(Guid.NewGuid()),
        BorrowDate = DateTime.UtcNow.AddDays(-1),
        Deadline = deadline ?? DateTime.UtcNow.AddDays(14),
        ReturnDate = null,
        RoomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Other Book", Author = "Other" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Other Room" }
        }
    };

    [Test]
    public async Task Execute_WhenApproved_ReservesCopyAndSetsExpiresAt()
    {
        // Arrange
        var request = NewPendingRequest(bookCount: 5, borrowedCount: 1, reservedCount: 0);
        var (command, requestRepo, _, audit, _, unitOfWork) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var beforeExecute = DateTime.UtcNow;

        // Act
        var response = await command.Execute(
            new ApproveBorrowRequest { BorrowRequestId = request.Id.Value },
            CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Approved));
            Assert.That(request.ProcessedAt, Is.Not.Null);
            Assert.That(request.ExpiresAt, Is.Not.Null);

            var expectedMin = beforeExecute.AddHours(ReserveHoldHours);
            var expectedMax = DateTime.UtcNow.AddHours(ReserveHoldHours);
            Assert.That(request.ExpiresAt, Is.InRange(expectedMin, expectedMax));

            // Бронь: ReservedCount инкрементирован, AvailableCount уменьшился.
            Assert.That(request.RoomBook.ReservedCount, Is.EqualTo(1));
            Assert.That(request.RoomBook.AvailableCount, Is.EqualTo(5 - 1 - 1));

            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

            audit.Verify(a => a.LogAsync(
                It.IsAny<AuditActionType>(),
                nameof(BorrowRequest),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<Id?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenApproved_SendsEmailAfterCommitWithoutWarnings()
    {
        // Arrange
        var request = NewPendingRequest();
        var (command, requestRepo, notification, _, _, unitOfWork) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await command.Execute(
            new ApproveBorrowRequest { BorrowRequestId = request.Id.Value },
            CancellationToken.None);

        // Assert: письмо с пустым списком warnings (нет лимита, нет просрочек).
        notification.Verify(n => n.SendBorrowRequestApprovedAsync(
            It.IsAny<User>(),
            It.IsAny<BorrowRequest>(),
            It.Is<IReadOnlyCollection<string>>(w => w.Count == 0),
            It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Execute_WhenUserAtLimit_EmailIncludesWarning()
    {
        // Arrange: 5 активных книг при лимите 5 — лимит достигнут.
        var activeBorrows = new List<UserRoomBook>();
        for (var i = 0; i < 5; i++)
            activeBorrows.Add(NewActiveBorrow());

        var request = NewPendingRequest(userActiveBorrows: activeBorrows);
        var (command, requestRepo, notification, _, _, _) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await command.Execute(
            new ApproveBorrowRequest { BorrowRequestId = request.Id.Value },
            CancellationToken.None);

        // Assert
        notification.Verify(n => n.SendBorrowRequestApprovedAsync(
            It.IsAny<User>(),
            It.IsAny<BorrowRequest>(),
            It.Is<IReadOnlyCollection<string>>(w => w.Any(s => s.Contains("Верните хотя бы одну"))),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Execute_WhenUserHasOverdue_EmailIncludesWarning()
    {
        // Arrange: одна активная книга с истёкшим deadline.
        var overdue = NewActiveBorrow(deadline: DateTime.UtcNow.AddDays(-3));
        var request = NewPendingRequest(userActiveBorrows: [overdue]);
        var (command, requestRepo, notification, _, _, _) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await command.Execute(
            new ApproveBorrowRequest { BorrowRequestId = request.Id.Value },
            CancellationToken.None);

        // Assert
        notification.Verify(n => n.SendBorrowRequestApprovedAsync(
            It.IsAny<User>(),
            It.IsAny<BorrowRequest>(),
            It.Is<IReadOnlyCollection<string>>(w => w.Any(s => s.Contains("просроченные"))),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Execute_WhenEmailThrows_DoesNotRollbackApproval()
    {
        // Arrange
        var request = NewPendingRequest();
        var (command, requestRepo, notification, _, logger, unitOfWork) = CreateCommand();

        notification
            .Setup(n => n.SendBorrowRequestApprovedAsync(
                It.IsAny<User>(),
                It.IsAny<BorrowRequest>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SmtpException("Simulated SMTP failure"));

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act — не должно бросать
        var response = await command.Execute(
            new ApproveBorrowRequest { BorrowRequestId = request.Id.Value },
            CancellationToken.None);

        // Assert: бронь активна, коммит был, email упал с LogError.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Approved));

            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

            logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("approval email failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenNoAvailableCopies_ThrowsWithoutReserving()
    {
        // Arrange: всё выдано и/или забронировано.
        var request = NewPendingRequest(bookCount: 5, borrowedCount: 4, reservedCount: 1);
        var (command, requestRepo, notification, _, _, unitOfWork) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new ApproveBorrowRequest { BorrowRequestId = request.Id.Value },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Member("Нет доступных экземпляров для брони"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Pending));
            Assert.That(request.RoomBook.ReservedCount, Is.EqualTo(1));

            notification.Verify(n => n.SendBorrowRequestApprovedAsync(
                It.IsAny<User>(),
                It.IsAny<BorrowRequest>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenStatusNotPending_ThrowsWithoutCommitting()
    {
        // Arrange
        var request = NewPendingRequest();
        request.Status = BorrowRequestStatus.Approved;

        var (command, requestRepo, _, _, _, unitOfWork) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new ApproveBorrowRequest { BorrowRequestId = request.Id.Value },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains("уже обработана"));
            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Test]
    public async Task Execute_WhenRequestNotFound_Throws()
    {
        // Arrange
        var (command, requestRepo, _, _, _, _) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BorrowRequest?)null);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {

            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
            new ApproveBorrowRequest { BorrowRequestId = Guid.NewGuid() },
            CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Member("Заявка не найдена"));
        }
    }
}