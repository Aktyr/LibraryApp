namespace LibApp.ApplicationTests.Commands.Entities.BorrowRequests;

[TestFixture]
public class CreateBorrowRequestCommandTests
{
    private const int MaxActiveRequests = 5;

    private static (CreateBorrowRequestCommand Command,
                    Mock<IRepository<User>> UserRepo,
                    Mock<IRepository<RoomBook>> RoomBookRepo,
                    Mock<IRepository<BorrowRequest>> RequestRepo,
                    Mock<IAuditService> Audit,
                    Mock<IUnitOfWork> UnitOfWork)
        CreateCommand(Guid currentUserId)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var userRepo = new Mock<IRepository<User>>();
        var roomBookRepo = new Mock<IRepository<RoomBook>>();
        var requestRepo = new Mock<IRepository<BorrowRequest>>();

        unitOfWork.Setup(u => u.GetRepository<User>()).Returns(userRepo.Object);
        unitOfWork.Setup(u => u.GetRepository<RoomBook>()).Returns(roomBookRepo.Object);
        unitOfWork.Setup(u => u.GetRepository<BorrowRequest>()).Returns(requestRepo.Object);

        // Транзакция: Begin → action → SaveChanges → Commit, catch → Rollback → rethrow.
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
        currentUser.SetupGet(c => c.LibraryUserId).Returns(new Id(currentUserId));

        var settings = new BorrowingSettings
        {
            MaxActiveRequestsPerUser = MaxActiveRequests
        };
        var settingsSnapshot = new Mock<IOptionsSnapshot<BorrowingSettings>>();
        settingsSnapshot.SetupGet(s => s.Value).Returns(settings);

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

        var command = new CreateBorrowRequestCommand(
            unitOfWork.Object,
            currentUser.Object,
            TimeProvider.System,
            settingsSnapshot.Object,
            audit.Object);

        return (command, userRepo, roomBookRepo, requestRepo, audit, unitOfWork);
    }

    private static User NewActiveUser(Guid id) => new()
    {
        Id = new Id(id),
        LastName = "Иванов",
        FirstName = "Иван",
        MiddleName = "Иванович",
        Email = "ivanov@example.com",
        ContactInfo = "ivanov@example.com",
        SubscriptionStatus = SubscriptionStatus.Active,
        RoomBooks = []
    };

    private static RoomBook NewRoomBook(int bookCount = 5, int borrowed = 0, int reserved = 0) => new()
    {
        Id = new Id(Guid.NewGuid()),
        BookCount = bookCount,
        BorrowedCount = borrowed,
        ReservedCount = reserved,
        Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book", Author = "Test Author" },
        Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
    };

    [Test]
    public async Task Execute_WithValidData_CreatesPendingRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = NewActiveUser(userId);
        var roomBook = NewRoomBook(bookCount: 3, borrowed: 0, reserved: 0);

        var (command, userRepo, roomBookRepo, requestRepo, audit, unitOfWork) =
            CreateCommand(userId);

        userRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        roomBookRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RoomBook, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomBook);

        // CountAsync — активных заявок нет, лимит не достигнут.
        requestRepo.Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        // AnyAsync — дубликата нет.
        requestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        BorrowRequest? addedRequest = null;
        requestRepo.Setup(r => r.AddAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Callback<BorrowRequest, CancellationToken>((br, _) => addedRequest = br)
            .Returns(Task.CompletedTask);

        // Act
        var response = await command.Execute(
            new CreateBorrowRequest { RoomBookId = roomBook.Id.Value },
            CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(addedRequest, Is.Not.Null);
            Assert.That(addedRequest!.Status, Is.EqualTo(BorrowRequestStatus.Pending));
            Assert.That(addedRequest.UserId.Value, Is.EqualTo(userId));
            Assert.That(addedRequest.RoomBookId.Value, Is.EqualTo(roomBook.Id.Value));

            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            audit.Verify(a => a.LogAsync(
                AuditActionType.Create,
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
    public async Task Execute_WhenAtActiveRequestLimit_Throws()
    {
        // Arrange: активных заявок уже MaxActiveRequestsPerUser.
        var userId = Guid.NewGuid();
        var user = NewActiveUser(userId);
        var roomBook = NewRoomBook();

        var (command, userRepo, roomBookRepo, requestRepo, _, unitOfWork) =
            CreateCommand(userId);

        userRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        roomBookRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RoomBook, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomBook);
        requestRepo.Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MaxActiveRequests);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new CreateBorrowRequest { RoomBookId = roomBook.Id.Value },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains($"{MaxActiveRequests} из {MaxActiveRequests}"));
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenAlreadyHasActiveRequestForSameBook_Throws()
    {
        // Arrange: CountAsync вернул 1 (лимит не достигнут), AnyAsync — дубликат.
        var userId = Guid.NewGuid();
        var user = NewActiveUser(userId);
        var roomBook = NewRoomBook();

        var (command, userRepo, roomBookRepo, requestRepo, _, _) =
            CreateCommand(userId);

        userRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        roomBookRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RoomBook, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomBook);
        requestRepo.Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        requestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new CreateBorrowRequest { RoomBookId = roomBook.Id.Value },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Member("Заявка на эту книгу уже создана или одобрена"));
        }
    }

    [Test]
    public async Task Execute_WhenUserFrozen_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = NewActiveUser(userId);
        user.SubscriptionStatus = SubscriptionStatus.Frozen;

        var (command, userRepo, _, _, _, _) = CreateCommand(userId);

        userRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new CreateBorrowRequest { RoomBookId = Guid.NewGuid() },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains("заморожен"));
        }
    }

    [Test]
    public async Task Execute_WhenNoAvailableCopies_Throws()
    {
        // Arrange: все экземпляры выданы и/или забронированы.
        var userId = Guid.NewGuid();
        var user = NewActiveUser(userId);
        var roomBook = NewRoomBook(bookCount: 3, borrowed: 2, reserved: 1);

        var (command, userRepo, roomBookRepo, _, _, _) = CreateCommand(userId);

        userRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        roomBookRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RoomBook, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roomBook);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new CreateBorrowRequest { RoomBookId = roomBook.Id.Value },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Member("Нет доступных экземпляров книги в этой комнате"));
        }
    }

    [Test]
    public async Task Execute_WhenRoomBookNotFound_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = NewActiveUser(userId);

        var (command, userRepo, roomBookRepo, _, _, _) = CreateCommand(userId);

        userRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        roomBookRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RoomBook, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoomBook?)null);

        // Act & Assert
        await Assert.ThrowsAsync<RoomBookNotFoundException>(() => command.Execute(
            new CreateBorrowRequest { RoomBookId = Guid.NewGuid() },
            CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenUserNotFound_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (command, userRepo, _, _, _, _) = CreateCommand(userId);

        userRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => command.Execute(
            new CreateBorrowRequest { RoomBookId = Guid.NewGuid() },
            CancellationToken.None));
    }
}