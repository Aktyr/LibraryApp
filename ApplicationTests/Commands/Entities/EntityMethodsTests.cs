namespace LibApp.ApplicationTests.Commands.Entities;

[TestFixture]
public class EntityMethodsTests
{
    [Test]
    public void Book_ToString_ReturnsFormattedString()
    {
        // Arrange
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Война и мир",
            Author = "Лев Толстой",
            Year = 1869,
            Publisher = "Русский вестник"
        };

        // Act
        var result = book.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("Название: Война и мир\nАвтор: Лев Толстой\nГод: 1869"));
    }

    [Test]
    public void Room_SumOfBooks_ReturnsSumOfBookCounts()
    {
        // Arrange
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Test Room",
            RoomBooks =
            [
                new() { Id = new Id(Guid.NewGuid()), BookCount = 5, Book = new Book { Id = new Id(Guid.NewGuid()), Title = "B1" }, Room = new Room { Id = new Id(Guid.NewGuid()) } },
                new() { Id = new Id(Guid.NewGuid()), BookCount = 3, Book = new Book { Id = new Id(Guid.NewGuid()), Title = "B2" }, Room = new Room { Id = new Id(Guid.NewGuid()) } },
                new() { Id = new Id(Guid.NewGuid()), BookCount = 7, Book = new Book { Id = new Id(Guid.NewGuid()), Title = "B3" }, Room = new Room { Id = new Id(Guid.NewGuid()) } }
            ]
        };

        // Act
        var sum = room.SumOfBooks;

        // Assert
        Assert.That(sum, Is.EqualTo(15));
    }

    [Test]
    public void Room_SumOfBooks_WhenEmpty_ReturnsZero()
    {
        // Arrange
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Empty Room",
            RoomBooks = []
        };

        // Act
        var sum = room.SumOfBooks;

        // Assert
        Assert.That(sum, Is.EqualTo(0));
    }

    [Test]
    public void User_ToString_ReturnsFormattedString()
    {
        // Arrange
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "ivanov@test.com"
        };

        // Act
        var result = user.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("Иванов Иван Иванович, ivanov@test.com"));
    }

    [Test]
    public void User_ToString_WhenMiddleNameEmpty_ReturnsFormattedString()
    {
        // Arrange
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Петров",
            FirstName = "Петр",
            MiddleName = "",
            ContactInfo = "+7-999-123-45-67"
        };

        // Act
        var result = user.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("Петров Петр , +7-999-123-45-67"));
    }

    [Test]
    public void BorrowBookRequest_Deadline_ReturnsCorrectDate()
    {
        // Arrange
        var request = new BorrowBookRequest
        {
            UserId = Guid.NewGuid(),
            RoomBookId = Guid.NewGuid(),
            BorrowDays = 14
        };

        // Act
        var deadline = request.Deadline;

        // Assert
        Assert.That(deadline, Is.EqualTo(DateTime.Now.AddDays(14)).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void ExtendDeadlineRequest_NewDeadline_ReturnsCorrectDate()
    {
        // Arrange
        var request = new ExtendDeadlineRequest
        {
            UserRoomBookId = Guid.NewGuid(),
            ExtraDays = 7
        };

        // Act
        var newDeadline = request.NewDeadline;

        // Assert
        Assert.That(newDeadline, Is.EqualTo(DateTime.Now.AddDays(7)).Within(TimeSpan.FromSeconds(5)));
    }
}