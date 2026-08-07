namespace LibApp.ApplicationTests.Commands.Entities.Books;

[TestFixture]
public class DiscardBookCommandTests
{
    [Test]
    public async Task Execute_WithValidData_DiscardsBooksSuccessfully()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var bookId = new Id(Guid.NewGuid());
        var roomId = new Id(Guid.NewGuid());

        var book = new Book
        {
            Id = bookId,
            Title = "Test Book",
            Author = "Author",
            RoomBook = new List<RoomBook>()
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = new Room { Id = roomId, Name = "Room" },
            BookCount = 10,
            BorrowedCount = 2 // 8 доступно
        };
        book.RoomBook.Add(roomBook);

        await bookRepo.AddRangeAsync(new[] { book }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new DiscardBookCommand(unitOfWork);
        var request = new DiscardBookRequest
        {
            BookId = bookId.Value,
            Amount = 3,
            DiscardReason = DiscardReason.Wear,
            ApprovedBy = "Admin",
            CompensationAmount = null
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Списано 3 экз."));

            var updatedRoomBook = (await roomBookRepo.GetAsync()).First();
            Assert.That(updatedRoomBook.BookCount, Is.EqualTo(7));

            var discardedEntries = await discardedRepo.GetAsync();
            Assert.That(discardedEntries.Count(), Is.EqualTo(1));
            var discarded = discardedEntries.First();
            Assert.That(discarded.Amount, Is.EqualTo(3));
            Assert.That(discarded.DiscardReason, Is.EqualTo(DiscardReason.Wear));
            Assert.That(discarded.ApprovedBy, Is.EqualTo("Admin"));
        });
    }

    [Test]
    public async Task Execute_WhenAmountIsZeroOrNegative_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var command = new DiscardBookCommand(unitOfWork);
        var request = new DiscardBookRequest
        {
            BookId = Guid.NewGuid(),
            Amount = 0,
            DiscardReason = DiscardReason.Wear
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Has.Member("Количество списываемых экземпляров должно быть больше нуля"));
    }

    [Test]
    public async Task Execute_WhenBookNotFound_ThrowsBookNotFoundException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var command = new DiscardBookCommand(unitOfWork);
        var request = new DiscardBookRequest
        {
            BookId = Guid.NewGuid(),
            Amount = 1,
            DiscardReason = DiscardReason.Wear
        };

        // Act & Assert
        Assert.ThrowsAsync<BookNotFoundException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenAmountExceedsTotalCopies_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var bookId = new Id(Guid.NewGuid());
        var roomId = new Id(Guid.NewGuid());

        var book = new Book { Id = bookId, Title = "Book", RoomBook = new List<RoomBook>() };
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = new Room { Id = roomId },
            BookCount = 5,
            BorrowedCount = 0
        };
        book.RoomBook.Add(roomBook);

        await bookRepo.AddRangeAsync(new[] { book }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new DiscardBookCommand(unitOfWork);
        var request = new DiscardBookRequest
        {
            BookId = bookId.Value,
            Amount = 10, // больше 5
            DiscardReason = DiscardReason.Wear
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Has.Member("Нельзя списать 10 экз. Доступно: 5"));
    }

    [Test]
    public async Task Execute_WhenAmountExceedsAvailableCopies_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var bookId = new Id(Guid.NewGuid());
        var roomId = new Id(Guid.NewGuid());

        var book = new Book { Id = bookId, Title = "Book", RoomBook = new List<RoomBook>() };
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = new Room { Id = roomId },
            BookCount = 5,
            BorrowedCount = 4 // доступно 1
        };
        book.RoomBook.Add(roomBook);

        await bookRepo.AddRangeAsync(new[] { book }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new DiscardBookCommand(unitOfWork);
        var request = new DiscardBookRequest
        {
            BookId = bookId.Value,
            Amount = 2, // больше доступных (1)
            DiscardReason = DiscardReason.Wear
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Has.Member("Нельзя списать 2 экз. Выдано: 4, доступно: 1"));
    }

    [Test]
    public async Task Execute_WhenApprovedByTooLong_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var bookId = new Id(Guid.NewGuid());
        var roomId = new Id(Guid.NewGuid());

        var book = new Book { Id = bookId, Title = "Book", RoomBook = new List<RoomBook>() };
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = new Room { Id = roomId },
            BookCount = 10,
            BorrowedCount = 0
        };
        book.RoomBook.Add(roomBook);

        await bookRepo.AddRangeAsync(new[] { book }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new DiscardBookCommand(unitOfWork);
        var request = new DiscardBookRequest
        {
            BookId = bookId.Value,
            Amount = 1,
            DiscardReason = DiscardReason.Wear,
            ApprovedBy = new string('A', 101) // 101 символ
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Has.Member("ApprovedBy не может превышать 100 символов"));
    }

    [Test]
    public async Task Execute_WithCompensationAmount_SetsCompensation()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var bookId = new Id(Guid.NewGuid());
        var roomId = new Id(Guid.NewGuid());
        var book = new Book { Id = bookId, Title = "Book", RoomBook = new List<RoomBook>() };
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = book, Room = new Room { Id = roomId }, BookCount = 10, BorrowedCount = 0 };
        book.RoomBook.Add(roomBook);
        await bookRepo.AddRangeAsync(new[] { book }, CancellationToken.None);
        await roomBookRepo.AddRangeAsync(new[] { roomBook }, CancellationToken.None);

        var command = new DiscardBookCommand(unitOfWork);
        var request = new DiscardBookRequest { BookId = bookId.Value, Amount = 3, DiscardReason = DiscardReason.Loss, ApprovedBy = "Admin", CompensationAmount = 150.50m };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        var discarded = (await discardedRepo.GetAsync()).First();
        Assert.That(discarded.CompensationAmount, Is.EqualTo(150.50m));
    }

    [Test]
    public async Task Execute_WhenSaveFails_RollsBackTransaction()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var discardedRepoMock = new Mock<IRepository<DiscardedBook>>();
        var roomBookRepoMock = new Mock<IRepository<RoomBook>>();

        unitOfWorkMock.Setup(u => u.GetRepository<DiscardedBook>()).Returns(discardedRepoMock.Object);
        unitOfWorkMock.Setup(u => u.GetRepository<RoomBook>()).Returns(roomBookRepoMock.Object);

        var discardedId = Guid.NewGuid();
        var discarded = new DiscardedBook
        {
            Id = new Id(discardedId),
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book" },
            RoomId = new Id(Guid.NewGuid()),
            Amount = 3
        };
        var roomBook = new RoomBook { Id = new Id(Guid.NewGuid()), Book = discarded.Book, BookCount = 10 };

        discardedRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<DiscardedBook, bool>>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(discarded);
        roomBookRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RoomBook, bool>>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(roomBook);

        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new Exception("DB error"));

        var command = new UndoDiscardCommand(unitOfWorkMock.Object);
        var request = new UndoDiscardRequest { DiscardId = discardedId, UndoReason = "Test" };

        // Act & Assert
        Assert.ThrowsAsync<Exception>(() => command.Execute(request, CancellationToken.None));

        unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

}