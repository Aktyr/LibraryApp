namespace LibApp.ApplicationTests.Commands.Entities.BorrowRequests;

[TestFixture]
public class ApproveBorrowRequestCommandTests
{
    // Мок репозитория BorrowRequest возвращается наружу: тесты настраивают его поведение
    // после CreateCommand. Иначе получить доступ к нему можно только через
    // unitOfWork.Object.GetRepository<BorrowRequest>(), а там уже Castle-прокси,
    // не приводимый обратно к Mock<>.
    private static (ApproveBorrowRequestCommand Command,
                    Mock<IRepository<BorrowRequest>> RequestRepo,
                    Mock<IBorrowingService> Borrowing,
                    Mock<IAuditService> Audit,
                    Mock<IUnitOfWork> UnitOfWork)
        CreateCommand(UserRoomBook? borrowResult = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var requestRepo = new Mock<IRepository<BorrowRequest>>();

        unitOfWork.Setup(u => u.GetRepository<BorrowRequest>()).Returns(requestRepo.Object);

        // Транзакция: Begin → action → SaveChanges → Commit. Catch → Rollback → rethrow.
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

        var borrowing = new Mock<IBorrowingService>();
        if (borrowResult is not null)
        {
            borrowing
                .Setup(b => b.BorrowAsync(
                    It.IsAny<Id>(), It.IsAny<Id>(), It.IsAny<int>(),
                    It.IsAny<Id?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(borrowResult);
        }

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.LibraryUserId).Returns(new Id(Guid.NewGuid()));

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
            borrowing.Object,
            currentUser.Object,
            TimeProvider.System,
            audit.Object);

        return (command, requestRepo, borrowing, audit, unitOfWork);
    }

    private static BorrowRequest NewPendingRequest() => new()
    {
        Id = new Id(Guid.NewGuid()),
        UserId = new Id(Guid.NewGuid()),
        RoomBookId = new Id(Guid.NewGuid()),
        Status = BorrowRequestStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    private static UserRoomBook NewUserRoomBook(BorrowRequest request) => new()
    {
        Id = new Id(Guid.NewGuid()),
        BorrowRequestId = request.Id,
        Deadline = DateTime.UtcNow.AddDays(14),
        BorrowDate = DateTime.UtcNow,
        User = new User
        {
            Id = request.UserId,
            LastName = "Иванов",
            FirstName = "Иван",
            Email = "ivanov@example.com"
        },
        RoomBook = new RoomBook
        {
            Id = request.RoomBookId,
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book", Author = "Test Author" },
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" }
        }
    };

    [Test]
    public async Task Execute_WhenApproved_CommitsTransactionAndWritesBothAudits()
    {
        // Arrange
        var request = NewPendingRequest();
        var borrow = NewUserRoomBook(request);

        var (command, requestRepo, _, audit, unitOfWork) = CreateCommand(borrowResult: borrow);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await command.Execute(
            new ApproveBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
            CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Approved));
            Assert.That(request.ProcessedAt, Is.Not.Null);

            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Второй аудит — выдача, помимо базового ApproveBorrowRequest.
            audit.Verify(a => a.LogAsync(
                AuditActionType.Borrow,
                nameof(UserRoomBook),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<Id?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenStatusNotPending_ThrowsWithoutCommitting()
    {
        // Arrange
        var request = NewPendingRequest();
        request.Status = BorrowRequestStatus.Approved;

        var (command, requestRepo, _, _, unitOfWork) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new ApproveBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains("уже обработана"));
            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Test]
    public async Task Execute_WhenRequestNotFound_Throws()
    {
        // Arrange
        var (command, requestRepo, _, _, _) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BorrowRequest?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
            new ApproveBorrowRequest { BorrowRequestId = Guid.NewGuid(), BorrowDays = 14 },
            CancellationToken.None));

        Assert.That(ex.ExceptionDetails, Has.Member("Заявка не найдена"));
    }

    [Test]
    public async Task Execute_WhenBorrowingServiceThrows_RollsBackWithoutChangingStatus()
    {
        // Arrange
        var request = NewPendingRequest();

        var (command, requestRepo, borrowing, _, unitOfWork) = CreateCommand();

        borrowing
            .Setup(b => b.BorrowAsync(
                It.IsAny<Id>(), It.IsAny<Id>(), It.IsAny<int>(),
                It.IsAny<Id?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LibValidationException { ExceptionDetails = ["Нет доступных экземпляров"] });

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new ApproveBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Member("Нет доступных экземпляров"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Pending));
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}