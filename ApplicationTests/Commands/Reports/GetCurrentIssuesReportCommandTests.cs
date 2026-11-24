namespace LibApp.ApplicationTests.Commands.Reports;

[TestFixture]
public class GetCurrentIssuesReportCommandTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Execute_WhenAllBooksReturned_ReturnsEmptyReport()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var repo = uow.GetRepository<UserRoomBook>();
        var reader = NewReader("Иванов");
        var roomBook = NewRoomBook("Book 1", "Author 1", "Room A");
        await repo.AddRangeAsync(
            [NewIssue(reader, roomBook, Now.AddDays(-10), Now.AddDays(-3), returnDate: Now.AddDays(-2))],
            CancellationToken.None);

        // Act
        var response = await CreateCommand(uow).Execute(new EmptyRequest(), CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Data, Is.Empty);
        }
    }

    [Test]
    public async Task Execute_ReturnsOnlyActiveIssuesWithMappedFields()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var repo = uow.GetRepository<UserRoomBook>();
        var reader = NewReader("Иванов");
        var active = NewIssue(reader, NewRoomBook("Clean Code", "Martin", "Room A"), Now.AddDays(-5), Now.AddDays(9));
        var returned = NewIssue(reader, NewRoomBook("Refactoring", "Fowler", "Room B"), Now.AddDays(-20), Now.AddDays(-6), returnDate: Now.AddDays(-4));
        await repo.AddRangeAsync([active, returned], CancellationToken.None);

        // Act
        var response = await CreateCommand(uow).Execute(new EmptyRequest(), CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Data, Has.Length.EqualTo(1));
            var row = response.Data[0];
            Assert.That(row.UserRoomBookId, Is.EqualTo(active.Id.Value));
            Assert.That(row.UserId, Is.EqualTo(reader.Id.Value));
            Assert.That(row.UserFullName, Is.EqualTo("Иванов Иван Иванович"));
            Assert.That(row.IsUserDeleted, Is.False);
            Assert.That(row.BookTitle, Is.EqualTo("Clean Code"));
            Assert.That(row.BookAuthor, Is.EqualTo("Martin"));
            Assert.That(row.RoomName, Is.EqualTo("Room A"));
            Assert.That(row.BorrowDate, Is.EqualTo(active.BorrowDate));
            Assert.That(row.Deadline, Is.EqualTo(active.Deadline));
        }
    }

    [Test]
    public async Task Execute_IncludesDeletedReaders()
    {
        // Arrange: у деактивированного читателя книга осталась на руках (legacy-данные или правка БД).
        var uow = new FakeUnitOfWork();
        var repo = uow.GetRepository<UserRoomBook>();
        var activeReader = NewReader("Иванов");
        var deletedReader = NewReader("Петров", isDeleted: true);
        await repo.AddRangeAsync(
        [
            NewIssue(activeReader, NewRoomBook("Book 1", "Author 1", "Room A"), Now.AddDays(-3), Now.AddDays(5)),
            NewIssue(deletedReader, NewRoomBook("Book 2", "Author 2", "Room B"), Now.AddDays(-7), Now.AddDays(2))
        ], CancellationToken.None);

        // Act
        var response = await CreateCommand(uow).Execute(new EmptyRequest(), CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Data, Has.Length.EqualTo(2));
            var deletedRow = response.Data.Single(r => r.UserId == deletedReader.Id.Value);
            Assert.That(deletedRow.IsUserDeleted, Is.True);
            Assert.That(deletedRow.UserFullName, Is.EqualTo("Петров Иван Иванович"));
        }
    }

    [Test]
    public async Task Execute_ComputesOverdueDaysAndKeepsStoredPenalty()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var repo = uow.GetRepository<UserRoomBook>();
        var reader = NewReader("Иванов");
        var overdue = NewIssue(reader, NewRoomBook("Overdue", "Author", "Room A"), Now.AddDays(-20), Now.AddDays(-3), penalty: 30m);
        var onTime = NewIssue(reader, NewRoomBook("On time", "Author", "Room A"), Now.AddDays(-2), Now.AddDays(5));
        var undated = NewIssue(reader, NewRoomBook("Undated", "Author", "Room A"), Now.AddDays(-1), deadline: null);
        await repo.AddRangeAsync([overdue, onTime, undated], CancellationToken.None);

        // Act
        var response = await CreateCommand(uow).Execute(new EmptyRequest(), CancellationToken.None);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            var overdueRow = response.Data.Single(r => r.UserRoomBookId == overdue.Id.Value);
            Assert.That(overdueRow.DaysOverdue, Is.EqualTo(3));
            Assert.That(overdueRow.Penalty, Is.EqualTo(30m));

            var onTimeRow = response.Data.Single(r => r.UserRoomBookId == onTime.Id.Value);
            Assert.That(onTimeRow.DaysOverdue, Is.EqualTo(0));
            // Штраф не начислен фоновым сервисом: остаётся null, отчёт его не вычисляет.
            Assert.That(onTimeRow.Penalty, Is.Null);

            var undatedRow = response.Data.Single(r => r.UserRoomBookId == undated.Id.Value);
            Assert.That(undatedRow.DaysOverdue, Is.EqualTo(0));
        }
    }

    [Test]
    public async Task Execute_SortsByNearestDeadlineWithUndatedLast()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var repo = uow.GetRepository<UserRoomBook>();
        var reader = NewReader("Иванов");
        var far = NewIssue(reader, NewRoomBook("Far", "Author", "Room A"), Now.AddDays(-1), Now.AddDays(10));
        var near = NewIssue(reader, NewRoomBook("Near", "Author", "Room A"), Now.AddDays(-2), Now.AddDays(1));
        var undated = NewIssue(reader, NewRoomBook("Undated", "Author", "Room A"), Now.AddDays(-3), deadline: null);
        await repo.AddRangeAsync([far, undated, near], CancellationToken.None);

        // Act
        var response = await CreateCommand(uow).Execute(new EmptyRequest(), CancellationToken.None);

        // Assert
        Assert.That(response.Data.Select(r => r.BookTitle), Is.EqualTo(["Near", "Far", "Undated"]));
    }

    #region Фикстуры

    private static GetCurrentIssuesReportCommand CreateCommand(FakeUnitOfWork uow)
        => new(uow, new FixedTimeProvider(new DateTimeOffset(Now)));

    private static User NewReader(string lastName, bool isDeleted = false) => new()
    {
        Id = new Id(Guid.NewGuid()),
        LastName = lastName,
        FirstName = "Иван",
        MiddleName = "Иванович",
        Email = $"{lastName}@test.local",
        IsDeleted = isDeleted
    };

    private static RoomBook NewRoomBook(string title, string author, string roomName)
    {
        var book = new Book { Id = new Id(Guid.NewGuid()), Title = title, Author = author };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = roomName };
        return new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookId = book.Id,
            Book = book,
            RoomId = room.Id,
            Room = room,
            BookCount = 1,
            BorrowedCount = 1
        };
    }

    private static UserRoomBook NewIssue(
        User reader,
        RoomBook roomBook,
        DateTime borrowDate,
        DateTime? deadline,
        DateTime? returnDate = null,
        decimal? penalty = null) => new()
        {
            Id = new Id(Guid.NewGuid()),
            UserId = reader.Id,
            User = reader,
            RoomBookId = roomBook.Id,
            RoomBook = roomBook,
            BorrowDate = borrowDate,
            Deadline = deadline,
            ReturnDate = returnDate,
            Penalty = penalty
        };

    // Фиксированное время: дни просрочки должны считаться детерминированно, без DateTime.UtcNow.
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    #endregion
}