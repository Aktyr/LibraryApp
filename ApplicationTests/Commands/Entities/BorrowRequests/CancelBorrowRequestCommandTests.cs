namespace LibApp.ApplicationTests.Commands.Entities.BorrowRequests;

[TestFixture]
public class CancelBorrowRequestCommandTests
{
    private static (CancelBorrowRequestCommand Command,
                    Mock<IRepository<BorrowRequest>> RequestRepo,
                    Mock<IAuditService> Audit,
                    Mock<IUnitOfWork> UnitOfWork)
        CreateCommand(Guid currentUserId)
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

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.LibraryUserId).Returns(new Id(currentUserId));

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

        var command = new CancelBorrowRequestCommand(
            unitOfWork.Object,
            currentUser.Object,
            TimeProvider.System,
            audit.Object);

        return (command, requestRepo, audit, unitOfWork);
    }

    // Собирает BorrowRequest с навигациями User и RoomBook.Book — нужны для текста аудита.
    private static BorrowRequest NewRequest(
        Guid userId,
        BorrowRequestStatus status,
        int bookCount = 5,
        int borrowedCount = 0,
        int reservedCount = 0)
    {
        var userIdValue = new Id(userId);

        var user = new User
        {
            Id = userIdValue,
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            Email = "ivanov@example.com"
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
            UserId = userIdValue,
            User = user,
            RoomBookId = roomBook.Id,
            RoomBook = roomBook,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = status == BorrowRequestStatus.Approved
                ? DateTime.UtcNow.AddHours(48)
                : null
        };
    }

    [Test]
    public async Task Execute_WhenOwnerCancelsPending_ChangesStatusWithoutTouchingReservedCount()
    {
        // Arrange: Pending — резерва не было, ReservedCount не должен измениться.
        var userId = Guid.NewGuid();
        var request = NewRequest(userId, BorrowRequestStatus.Pending, bookCount: 5, borrowedCount: 1, reservedCount: 0);

        var (command, requestRepo, audit, unitOfWork) = CreateCommand(userId);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await command.Execute(
            new CancelBorrowRequest { BorrowRequestId = request.Id.Value },
            CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Cancelled));
            Assert.That(request.ProcessedAt, Is.Not.Null);
            Assert.That(request.RoomBook.ReservedCount, Is.EqualTo(0));

            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            audit.Verify(a => a.LogAsync(
                AuditActionType.Update,
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
    public async Task Execute_WhenOwnerCancelsApproved_DecrementsReservedCount()
    {
        // Arrange: Approved — был резерв, декремент обязателен, иначе экземпляр «застрянет».
        var userId = Guid.NewGuid();
        var request = NewRequest(userId, BorrowRequestStatus.Approved, bookCount: 5, borrowedCount: 1, reservedCount: 2);

        var (command, requestRepo, _, _) = CreateCommand(userId);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await command.Execute(
            new CancelBorrowRequest { BorrowRequestId = request.Id.Value },
            CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Cancelled));
            Assert.That(request.RoomBook.ReservedCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task Execute_WhenRequestBelongsToAnotherUser_Throws()
    {
        // Arrange: currentUser — это один Guid, заявка принадлежит другому.
        var currentUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var request = NewRequest(ownerId, BorrowRequestStatus.Pending);

        var (command, requestRepo, _, unitOfWork) = CreateCommand(currentUserId);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new CancelBorrowRequest { BorrowRequestId = request.Id.Value },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Member("Отменить можно только свою заявку"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Pending));
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenStatusCompleted_Throws()
    {
        // Arrange: терминальный статус, отмена не применима.
        var userId = Guid.NewGuid();
        var request = NewRequest(userId, BorrowRequestStatus.Completed, bookCount: 5, borrowedCount: 1, reservedCount: 0);

        var (command, requestRepo, _, _) = CreateCommand(userId);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new CancelBorrowRequest { BorrowRequestId = request.Id.Value },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains("Нельзя отменить"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Completed));
        }
    }

    [Test]
    public async Task Execute_WhenStatusExpired_Throws()
    {
        // Arrange: Expired — терминальный статус, заявка уже закрыта фоновым сервисом.
        var userId = Guid.NewGuid();
        var request = NewRequest(userId, BorrowRequestStatus.Expired);

        var (command, requestRepo, _, _) = CreateCommand(userId);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
            new CancelBorrowRequest { BorrowRequestId = request.Id.Value },
            CancellationToken.None));

        Assert.That(ex.ExceptionDetails, Has.Some.Contains("Нельзя отменить"));
    }

    [Test]
    public async Task Execute_WithCommentTooLong_Throws()
    {
        // Arrange: комментарий превышает BorrowRequestPolicy.MaxCommentLength.
        var userId = Guid.NewGuid();
        var request = NewRequest(userId, BorrowRequestStatus.Pending);
        var tooLong = new string('x', BorrowRequestPolicy.MaxCommentLength + 1);

        var (command, requestRepo, _, _) = CreateCommand(userId);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new CancelBorrowRequest { BorrowRequestId = request.Id.Value, Comment = tooLong },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains($"{BorrowRequestPolicy.MaxCommentLength}"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Pending));
        }
    }

    [Test]
    public async Task Execute_WhenRequestNotFound_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (command, requestRepo, _, _) = CreateCommand(userId);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BorrowRequest?)null);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new CancelBorrowRequest { BorrowRequestId = Guid.NewGuid() },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Member("Заявка не найдена"));
        }
    }
}