namespace LibApp.ApplicationTests.Commands.Entities.BorrowRequests;

[TestFixture]
public class IssueBorrowRequestCommandTests
{
    private static (IssueBorrowRequestCommand Command,
                    Mock<IRepository<BorrowRequest>> RequestRepo,
                    Mock<IBorrowingService> Borrowing,
                    Mock<INotificationService> Notification,
                    Mock<IAuditService> Audit,
                    Mock<ILogger<IssueBorrowRequestCommand>> Logger,
                    Mock<IUnitOfWork> UnitOfWork)
        CreateCommand(UserRoomBook? borrowResult = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var requestRepo = new Mock<IRepository<BorrowRequest>>();

        unitOfWork.Setup(u => u.GetRepository<BorrowRequest>()).Returns(requestRepo.Object);

        // Транзакция: Begin → action → SaveChanges → Commit. Catch → Rollback → rethrow.
        // Порядок важен: в тесте на падение email Commit должен быть вызван ДО email,
        // иначе исключение email откатило бы выдачу и тест не отловил бы регрессию.
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

        var borrowing = new Mock<IBorrowingService>();
        if (borrowResult is not null)
        {
            borrowing
                .Setup(b => b.BorrowAsync(
                    It.IsAny<Id>(), It.IsAny<Id>(), It.IsAny<int>(),
                    It.IsAny<Id?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(borrowResult);
        }

        var notification = new Mock<INotificationService>();
        notification
            .Setup(n => n.SendBorrowConfirmationAsync(
                It.IsAny<User>(), It.IsAny<UserRoomBook>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<IssueBorrowRequestCommand>>();

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

        var command = new IssueBorrowRequestCommand(
            unitOfWork.Object,
            borrowing.Object,
            currentUser.Object,
            TimeProvider.System,
            notification.Object,
            logger.Object,
            audit.Object);

        return (command, requestRepo, borrowing, notification, audit, logger, unitOfWork);
    }

    private static BorrowRequest NewApprovedRequest(
        int bookCount = 5,
        int borrowedCount = 0,
        int reservedCount = 1,
        DateTime? expiresAt = null)
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
            RoomBooks = []
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
            Status = BorrowRequestStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ProcessedAt = DateTime.UtcNow.AddHours(-1),
            ProcessedByUserId = new Id(Guid.NewGuid()),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(48)
        };
    }

    private static UserRoomBook NewIssuedBorrow(BorrowRequest request) => new()
    {
        Id = new Id(Guid.NewGuid()),
        BorrowRequestId = request.Id,
        BorrowDate = DateTime.UtcNow,
        Deadline = DateTime.UtcNow.AddDays(14),
        User = request.User,
        RoomBook = request.RoomBook
    };

    [Test]
    public async Task Execute_WhenIssued_DecrementsReserveAndMarksCompleted()
    {
        // Arrange
        var request = NewApprovedRequest(bookCount: 5, borrowedCount: 1, reservedCount: 1);
        var issued = NewIssuedBorrow(request);
        var (command, requestRepo, _, _, audit, _, unitOfWork) = CreateCommand(borrowResult: issued);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await command.Execute(
            new IssueBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
            CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Completed));
            Assert.That(request.PickedUpAt, Is.Not.Null);

            // Бронь стала выдачей: ReservedCount -1, AvailableCount не изменился.
            Assert.That(request.RoomBook.ReservedCount, Is.EqualTo(0));
            Assert.That(request.RoomBook.AvailableCount, Is.EqualTo(5 - 1 - 0));

            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Аудит Borrow — один, ApproveBorrowRequest уже записан на шаге Approve.
            audit.Verify(a => a.LogAsync(
                It.IsAny<AuditActionType>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<Id?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenIssued_SendsConfirmationEmailAfterCommit()
    {
        // Arrange
        var request = NewApprovedRequest();
        var issued = NewIssuedBorrow(request);
        var (command, requestRepo, _, notification, _, _, unitOfWork) = CreateCommand(borrowResult: issued);

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await command.Execute(
            new IssueBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
            CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            notification.Verify(n => n.SendBorrowConfirmationAsync(
            It.IsAny<User>(), It.IsAny<UserRoomBook>(), It.IsAny<CancellationToken>()),
            Times.Once);
            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenEmailThrows_DoesNotRollbackIssue()
    {
        // Arrange
        var request = NewApprovedRequest();
        var issued = NewIssuedBorrow(request);
        var (command, requestRepo, _, notification, _, logger, unitOfWork) = CreateCommand(borrowResult: issued);

        notification
            .Setup(n => n.SendBorrowConfirmationAsync(
                It.IsAny<User>(), It.IsAny<UserRoomBook>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SmtpException("Simulated SMTP failure"));

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<BorrowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act — не должно бросать
        var response = await command.Execute(
            new IssueBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
            CancellationToken.None);

        // Assert: выдача создана, коммит был, email упал с LogError.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Completed));

            unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

            logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("confirmation email failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenReservationExpired_ThrowsWithoutIssuing()
    {
        // Arrange: ExpiresAt в прошлом. Фоновый сервис ещё не успел перевести в Expired —
        // Issue должен отказать сам, до вызова BorrowAsync.
        var request = NewApprovedRequest(expiresAt: DateTime.UtcNow.AddMinutes(-10));
        var (command, requestRepo, borrowing, notification, _, _, unitOfWork) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new IssueBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains("Бронь истекла"));

            // Состояние не тронуто, BorrowAsync не вызван.
            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Approved));
            Assert.That(request.RoomBook.ReservedCount, Is.EqualTo(1));

            borrowing.Verify(b => b.BorrowAsync(
                It.IsAny<Id>(), It.IsAny<Id>(), It.IsAny<int>(),
                It.IsAny<Id?>(), It.IsAny<CancellationToken>()),
                Times.Never);

            notification.Verify(n => n.SendBorrowConfirmationAsync(
                It.IsAny<User>(), It.IsAny<UserRoomBook>(), It.IsAny<CancellationToken>()),
                Times.Never);
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenBorrowingServiceThrows_KeepsReservationActive()
    {
        // Arrange: валидатор BorrowAsync отказал (например, лимит на руках достигнут).
        // Бронь остаётся Approved, ReservedCount не тронут: библиотекарь сможет повторить
        // попытку после того, как читатель вернёт книги.
        var request = NewApprovedRequest();
        var (command, requestRepo, borrowing, notification, _, _, unitOfWork) = CreateCommand();

        borrowing
            .Setup(b => b.BorrowAsync(
                It.IsAny<Id>(), It.IsAny<Id>(), It.IsAny<int>(),
                It.IsAny<Id?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LibValidationException
            {
                ExceptionDetails = ["Пользователь уже взял максимальное количество книг (5)"]
            });

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
                new IssueBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
                CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains("максимальное количество"));

            Assert.That(request.Status, Is.EqualTo(BorrowRequestStatus.Approved));
            Assert.That(request.RoomBook.ReservedCount, Is.EqualTo(1));
            Assert.That(request.PickedUpAt, Is.Null);

            notification.Verify(n => n.SendBorrowConfirmationAsync(
                It.IsAny<User>(), It.IsAny<UserRoomBook>(), It.IsAny<CancellationToken>()),
                Times.Never);
            unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Test]
    public async Task Execute_WhenStatusPending_Throws()
    {
        // Arrange: нельзя оформить выдачу по ещё не одобренной заявке.
        var request = NewApprovedRequest();
        request.Status = BorrowRequestStatus.Pending;
        request.ExpiresAt = null;

        var (command, requestRepo, _, _, _, _, _) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
            new IssueBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
            CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains("только по одобренной"));
        }
    }

    [Test]
    public async Task Execute_WhenStatusCompleted_Throws()
    {
        // Arrange: повторная выдача по уже закрытой заявке — ошибка клиента.
        var request = NewApprovedRequest();
        request.Status = BorrowRequestStatus.Completed;
        request.PickedUpAt = DateTime.UtcNow.AddDays(-1);

        var (command, requestRepo, _, _, _, _, _) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
            new IssueBorrowRequest { BorrowRequestId = request.Id.Value, BorrowDays = 14 },
            CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Some.Contains("только по одобренной"));
        }
    }

    [Test]
    public async Task Execute_WhenRequestNotFound_Throws()
    {
        // Arrange
        var (command, requestRepo, _, _, _, _, _) = CreateCommand();

        requestRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<BorrowRequest, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BorrowRequest?)null);

        // Act & Assert
        using (Assert.EnterMultipleScope())
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() => command.Execute(
            new IssueBorrowRequest { BorrowRequestId = Guid.NewGuid(), BorrowDays = 14 },
            CancellationToken.None));

            Assert.That(ex.ExceptionDetails, Has.Member("Заявка не найдена"));
        }
    }
}