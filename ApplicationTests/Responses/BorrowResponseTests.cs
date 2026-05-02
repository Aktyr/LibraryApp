namespace LibApp.ApplicationTests.Responses;

[TestFixture]
public class BorrowResponseTests
{
    [Test]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var status = "Ok";
        var message = "Список книг получен";
        var books = new[]
        {
            new BorrowedBookDTO(
                Id: Guid.NewGuid(),
                BookTitle: "Book 1",
                BookAuthor: "Author 1",
                RoomName: "Room A",
                Borrow: DateTime.Now,
                Deadline: DateTime.Now.AddDays(7),
                ReturnDate: null,
                Penalty: null
                ),
            new BorrowedBookDTO(
                Id: Guid.NewGuid(),
                BookTitle: "Book 2",
                BookAuthor: "Author 2",
                RoomName: "Room B",
                Borrow: DateTime.Now.AddDays(-5),
                Deadline: DateTime.Now.AddDays(2),
                ReturnDate: null,
                Penalty: 50m
            )
        };

        // Act
        var response = new BorrowResponse(status, message, books);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(status));
            Assert.That(response.Message, Is.EqualTo(message));
            Assert.That(response.Books, Is.SameAs(books));
            Assert.That(response.Books, Has.Length.EqualTo(2));
            Assert.That(response.Books[0].BookTitle, Is.EqualTo("Book 1"));
            Assert.That(response.Books[1].Penalty, Is.EqualTo(50m));
        });
    }

    [Test]
    public void Constructor_WithEmptyBooks_InitializesEmptyArray()
    {
        // Arrange
        var status = "Ok";
        var message = "No books found";
        var books = Array.Empty<BorrowedBookDTO>();

        // Act
        var response = new BorrowResponse(status, message, books);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(status));
            Assert.That(response.Message, Is.EqualTo(message));
            Assert.That(response.Books, Is.Empty);
            Assert.That(response.Books, Has.Length.EqualTo(0));
        });
    }
}