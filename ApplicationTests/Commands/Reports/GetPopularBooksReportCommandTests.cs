namespace LibApp.ApplicationTests.Commands.Reports;

[TestFixture]
public class GetPopularBooksReportCommandTests
{
    [Test]
    public async Task Execute_WithoutFilters_ReturnsAllBooksOrderedByBorrowCount()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1", Author = "A1" };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2", Author = "A2" };
        await bookRepo.AddRangeAsync(new[] { book1, book2 }, CancellationToken.None);

        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Room" };
        var rb1 = new RoomBook { Id = new Id(Guid.NewGuid()), Book = book1, Room = room, BookCount = 5, BorrowedCount = 0 };
        var rb2 = new RoomBook { Id = new Id(Guid.NewGuid()), Book = book2, Room = room, BookCount = 5, BorrowedCount = 0 };
        await roomBookRepo.AddRangeAsync(new[] { rb1, rb2 }, CancellationToken.None);

        var user = new User { Id = new Id(Guid.NewGuid()), Email = "u@test.com", LastName = "User" };
        // Создаём несколько выдач для book1
        var borrow1 = new UserRoomBook { Id = new Id(Guid.NewGuid()), User = user, RoomBook = rb1, BorrowDate = DateTime.UtcNow.AddDays(-5), Deadline = DateTime.UtcNow.AddDays(2) };
        var borrow2 = new UserRoomBook { Id = new Id(Guid.NewGuid()), User = user, RoomBook = rb1, BorrowDate = DateTime.UtcNow.AddDays(-3), Deadline = DateTime.UtcNow.AddDays(4) };
        // Одна выдача для book2
        var borrow3 = new UserRoomBook { Id = new Id(Guid.NewGuid()), User = user, RoomBook = rb2, BorrowDate = DateTime.UtcNow.AddDays(-1), Deadline = DateTime.UtcNow.AddDays(6) };
        await userRoomBookRepo.AddRangeAsync(new[] { borrow1, borrow2, borrow3 }, CancellationToken.None);

        var command = new GetPopularBooksReportCommand(unitOfWork);
        var request = new GetPopularBooksRequest();

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(2));
            // Первая книга должна быть book1 (2 выдачи)
            Assert.That(response.Data[0].BookId, Is.EqualTo(book1.Id.Value));
            Assert.That(response.Data[0].TotalBorrowedCount, Is.EqualTo(2));
            Assert.That(response.Data[0].CurrentBorrowedCount, Is.EqualTo(2)); // все активны (ReturnDate null)
            Assert.That(response.Data[1].BookId, Is.EqualTo(book2.Id.Value));
            Assert.That(response.Data[1].TotalBorrowedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Execute_WithDateRange_FiltersByDate()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" };
        await bookRepo.AddRangeAsync(new[] { book1 }, CancellationToken.None);
        var room = new Room { Id = new Id(Guid.NewGuid()) };
        var rb = new RoomBook { Id = new Id(Guid.NewGuid()), Book = book1, Room = room };
        await roomBookRepo.AddRangeAsync(new[] { rb }, CancellationToken.None);
        var user = new User { Id = new Id(Guid.NewGuid()), Email = "u@test.com", LastName = "User" };

        var now = DateTime.UtcNow;
        var borrowOld = new UserRoomBook { Id = new Id(Guid.NewGuid()), User = user, RoomBook = rb, BorrowDate = now.AddDays(-10) };
        var borrowNew = new UserRoomBook { Id = new Id(Guid.NewGuid()), User = user, RoomBook = rb, BorrowDate = now.AddDays(-2) };
        await userRoomBookRepo.AddRangeAsync(new[] { borrowOld, borrowNew }, CancellationToken.None);

        var command = new GetPopularBooksReportCommand(unitOfWork);
        var request = new GetPopularBooksRequest { FromDate = now.AddDays(-5) };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Data, Has.Length.EqualTo(1));
            Assert.That(response.Data[0].TotalBorrowedCount, Is.EqualTo(1)); // только новая
        });
    }

    [Test]
    public async Task Execute_WithTopCount_LimitsResult()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var books = Enumerable.Range(0, 5).Select(i => new Book { Id = new Id(Guid.NewGuid()), Title = $"Book {i}" }).ToList();
        await bookRepo.AddRangeAsync(books, CancellationToken.None);
        var room = new Room { Id = new Id(Guid.NewGuid()) };
        var rbs = books.Select(b => new RoomBook { Id = new Id(Guid.NewGuid()), Book = b, Room = room }).ToList();
        await roomBookRepo.AddRangeAsync(rbs, CancellationToken.None);
        var user = new User { Id = new Id(Guid.NewGuid()), Email = "u@test.com", LastName = "User" };

        // Даём каждой книге по одной выдаче, чтобы все были равны, но порядок не важен
        foreach (var rb in rbs)
        {
            await userRoomBookRepo.AddRangeAsync(new[] { new UserRoomBook { Id = new Id(Guid.NewGuid()), User = user, RoomBook = rb, BorrowDate = DateTime.UtcNow } }, CancellationToken.None);
        }

        var command = new GetPopularBooksReportCommand(unitOfWork);
        var request = new GetPopularBooksRequest { TopCount = 3 };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(response.Data, Has.Length.EqualTo(3));
    }
}