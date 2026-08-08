namespace LibApp.ApplicationTests.Exceptions;

[TestFixture]
public class ExceptionConstructorsTests
{
    [Test]
    public void BookNotFoundException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var ex = new BookNotFoundException("Custom book error");

        // Assert
        Assert.That(ex.Message, Is.EqualTo("Custom book error"));
    }

    [Test]
    public void RoomBookNotFoundException_DefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var ex = new RoomBookNotFoundException();

        // Assert
        Assert.That(ex.Message, Is.EqualTo("Book is not found in Room."));
    }

    [Test]
    public void RoomBookNotFoundException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var ex = new RoomBookNotFoundException("Custom room book error");

        // Assert
        Assert.That(ex.Message, Is.EqualTo("Custom room book error"));
    }

    [Test]
    public void RoomNotFoundException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var ex = new RoomNotFoundException("Custom room error");

        // Assert
        Assert.That(ex.Message, Is.EqualTo("Custom room error"));
    }

    [Test]
    public void RoomDeletionException_DefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var ex = new RoomDeletionException();

        // Assert
        Assert.That(ex.Message, Is.EqualTo("Room deletion error."));
    }

    [Test]
    public void UserNotFoundException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var ex = new UserNotFoundException("Custom user error");

        // Assert
        Assert.That(ex.Message, Is.EqualTo("Custom user error"));
    }

    [Test]
    public void UserRoomBookNotFoundException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var ex = new UserRoomBookNotFoundException("Custom borrow error");

        // Assert
        Assert.That(ex.Message, Is.EqualTo("Custom borrow error"));
    }

    [Test]
    public void UnauthorizedException_DefaultConstructor_SetsDefaultMessage()
    {
        // Arrange & Act
        var ex = new UnauthorizedException();

        // Assert
        Assert.That(ex.Message, Is.EqualTo("Unauthorized access"));
    }

    [Test]
    public void LibValidationException_Message_JoinsExceptionDetails()
    {
        // Arrange
        var ex = new LibValidationException
        {
            ExceptionDetails = ["Error 1", "Error 2", "Error 3"]
        };

        // Act
        var message = ex.Message;

        // Assert
        Assert.That(message, Is.EqualTo("Error 1\nError 2\nError 3"));
    }

    [Test]
    public void BorrowedBookDTO_ParameterlessConstructor_CreatesWithDefaults()
    {
        // Arrange & Act
        var dto = new BorrowedBookDTO();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(Guid.Empty));
            Assert.That(dto.BookTitle, Is.Null);
            Assert.That(dto.BookAuthor, Is.Null);
            Assert.That(dto.RoomName, Is.Null);
            Assert.That(dto.Borrow, Is.EqualTo(default(DateTime)));
            Assert.That(dto.Deadline, Is.Null);
            Assert.That(dto.ReturnDate, Is.Null);
            Assert.That(dto.Penalty, Is.Null);
        });
    }
}