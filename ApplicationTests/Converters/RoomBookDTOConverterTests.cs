namespace LibApp.ApplicationTests.Converters;

[TestFixture]
public class RoomBookDTOConverterTests
{
    [Test]
    public void ToEntity_ConvertsDtoToEntity_Success()
    {
        // Arrange
        var converter = new RoomBookDTOConverter();
        var roomId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var dto = new RoomBookDTO(
            Id: Guid.NewGuid(),
            RoomId: roomId,
            BookId: bookId,
            BookCount: 10
        );

        // Act
        var entity = converter.ToEntity(dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entity.Id.Value, Is.EqualTo(dto.Id));
            Assert.That(entity.BookCount, Is.EqualTo(dto.BookCount));
            Assert.That(entity.Room, Is.Not.Null);
            Assert.That(entity.Room.Id.Value, Is.EqualTo(roomId));
            Assert.That(entity.Book, Is.Not.Null);
            Assert.That(entity.Book.Id.Value, Is.EqualTo(bookId));
        });
    }

    [Test]
    public void ToEntity_WhenRoomIdIsEmpty_DoesNotCreateRoom()
    {
        // Arrange
        var converter = new RoomBookDTOConverter();
        var dto = new RoomBookDTO(
            Id: Guid.NewGuid(),
            RoomId: Guid.Empty,
            BookId: Guid.NewGuid(),
            BookCount: 10
        );

        // Act
        var entity = converter.ToEntity(dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entity.Room, Is.Null);
            Assert.That(entity.Book, Is.Not.Null);
        });
    }

    [Test]
    public void ToEntity_WhenBookIdIsEmpty_DoesNotCreateBook()
    {
        // Arrange
        var converter = new RoomBookDTOConverter();
        var dto = new RoomBookDTO(
            Id: Guid.NewGuid(),
            RoomId: Guid.NewGuid(),
            BookId: Guid.Empty,
            BookCount: 10
        );

        // Act
        var entity = converter.ToEntity(dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entity.Room, Is.Not.Null);
            Assert.That(entity.Book, Is.Null);
        });
    }

    [Test]
    public void ToDto_WhenRoomBookIsNull_ReturnsEmptyCollection()
    {
        // Arrange
        var converter = new BookDTOConverter();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Test",
            Author = "Author",
            RoomBook = null! // Явно null
        };

        // Act
        var dto = converter.ToDto(book);

        // Assert
        Assert.That(dto.RoomBook, Is.Not.Null);
        Assert.That(dto.RoomBook, Is.Empty);
    }

}
