namespace LibApp.ApplicationTests.Commands.Entities.Books;

[TestFixture]
public class UndoDiscardCommandTests
{
    [Test]
    public async Task Execute_WithValidData_UndoesDiscardSuccessfully()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var bookId = new Id(Guid.NewGuid());
        var roomId = new Id(Guid.NewGuid());
        var roomBookId = new Id(Guid.NewGuid());

        var book = new Book { Id = bookId, Title = "Test Book" };
        var room = new Room { Id = roomId, Name = "Room" };
        var roomBook = new RoomBook
        {
            Id = roomBookId,
            Book = book,
            Room = room,
            BookCount = 10,
            BorrowedCount = 2
        };

        var discarded = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = room,
            RoomId = roomId,
            Amount = 3,
            DiscardedDate = DateTime.UtcNow,
            DiscardReason = DiscardReason.Wear,
            ApprovedBy = "Admin"
        };

        await bookRepo.AddRangeAsync([book], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);
        await discardedRepo.AddRangeAsync([discarded], CancellationToken.None);

        var command = new UndoDiscardCommand(unitOfWork, Mock.Of<IAuditService>());
        var request = new UndoDiscardRequest
        {
            DiscardId = discarded.Id.Value,
            UndoReason = "Test undo",
            ApprovedBy = "Admin"
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Отменено списание 3 экз."));

            var updatedRoomBook = (await roomBookRepo.GetAsync()).First();
            Assert.That(updatedRoomBook.BookCount, Is.EqualTo(13));

            var discardedEntries = await discardedRepo.GetAsync();
            Assert.That(discardedEntries, Is.Empty);
        });
    }

    [Test]
    public async Task Execute_WhenDiscardRecordNotFound_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var command = new UndoDiscardCommand(unitOfWork, Mock.Of<IAuditService>());
        var request = new UndoDiscardRequest
        {
            DiscardId = Guid.NewGuid(),
            UndoReason = "Test"
        };

        // Act & Assert
        Assert.Multiple(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
                command.Execute(request, CancellationToken.None));
            Assert.That(ex.ExceptionDetails, Has.Member("Запись о списании не найдена"));
        });
    }

    [Test]
    public async Task Execute_WhenRoomBookNotFound_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var bookId = new Id(Guid.NewGuid());
        var roomId = new Id(Guid.NewGuid());

        var book = new Book { Id = bookId, Title = "Book" };
        var room = new Room { Id = roomId, Name = "Room" };

        var discarded = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = room,
            RoomId = roomId,
            Amount = 5,
            DiscardedDate = DateTime.UtcNow,
            DiscardReason = DiscardReason.Wear
        };

        await bookRepo.AddRangeAsync([book], CancellationToken.None);
        await discardedRepo.AddRangeAsync([discarded], CancellationToken.None);
        // RoomBook не добавляем

        var command = new UndoDiscardCommand(unitOfWork, Mock.Of<IAuditService>());
        var request = new UndoDiscardRequest
        {
            DiscardId = discarded.Id.Value,
            UndoReason = "Test"
        };

        // Act & Assert
        Assert.Multiple(async () =>
        {
            var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
                command.Execute(request, CancellationToken.None));
            Assert.That(ex.ExceptionDetails, Has.Member("Книга не найдена в комнате"));
        });
    }

    [Test]
    public async Task Execute_WithUndoReason_SetsReasonInResponse()
    {
        // Arrange (аналогично существующему тесту)
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var bookId = new Id(Guid.NewGuid());
        var roomId = new Id(Guid.NewGuid());
        var book = new Book { Id = bookId, Title = "Book" };
        var room = new Room { Id = roomId };
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = book, Room = room, BookCount = 10, BorrowedCount = 0 };
        var discarded = new DiscardedBook { Id = new Id(Guid.NewGuid()), Book = book, Room = room, RoomId = roomId, Amount = 3, DiscardedDate = DateTime.UtcNow, DiscardReason = DiscardReason.Wear };
        await bookRepo.AddRangeAsync([book], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);
        await discardedRepo.AddRangeAsync([discarded], CancellationToken.None);

        var command = new UndoDiscardCommand(unitOfWork, Mock.Of<IAuditService>());
        var request = new UndoDiscardRequest { DiscardId = discarded.Id.Value, UndoReason = "Test undo reason", ApprovedBy = "Admin" };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(response.Message, Does.Contain("Test undo reason"));
    }
    [Test]
    public async Task Execute_WhenSaveFails_PropagatesExceptionAndDoesNotAudit()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();
        var auditMock = new Mock<IAuditService>();

        var book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book" };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Room" };
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = room,
            BookCount = 10
        };
        var discarded = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = room,
            RoomId = room.Id,
            Amount = 3,
            DiscardedDate = DateTime.UtcNow,
            DiscardReason = DiscardReason.Wear
        }; 

        await bookRepo.AddRangeAsync([book], CancellationToken.None);
        await roomBookRepo.AddRangeAsync([roomBook], CancellationToken.None);
        await discardedRepo.AddRangeAsync([discarded], CancellationToken.None);

        var saveException = new InvalidOperationException("DB error");
        unitOfWork.SaveException = saveException;

        var command = new UndoDiscardCommand(unitOfWork, auditMock.Object);
        var request = new UndoDiscardRequest
        {
            DiscardId = discarded.Id.Value,
            UndoReason = "Test"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => command.Execute(request, CancellationToken.None));
        Assert.That(ex, Is.SameAs(saveException));

        // Аудит не должен фиксироваться, если сохранение упало (транзакция откатилась).
        auditMock.Verify(a => a.LogAsync(
            It.IsAny<AuditActionType>(), It.IsAny<string>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<Id?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}