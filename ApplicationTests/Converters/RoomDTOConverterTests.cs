namespace LibApp.ApplicationTests.Converters;

[TestFixture]
public class RoomDTOConverterTests
{
    [Test]
    public void ToDto_WhenRoomBooksIsEmpty_ReturnsEmptyRoomBookCollection()
    {
        // Arrange
        var converter = new RoomDTOConverter();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Test Room",
            RoomBooks = new List<RoomBook>()
        };

        // Act
        var dto = converter.ToDto(room);

        // Assert
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.RoomBook, Is.Not.Null);
        Assert.That(dto.RoomBook, Is.Empty);
        Assert.That(dto.Name, Is.EqualTo("Test Room"));
    }

    [Test]
    public void ToDto_WhenRoomBooksIsNull_ReturnsEmptyRoomBookCollection()
    {
        // Arrange
        var converter = new RoomDTOConverter();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Test Room",
            RoomBooks = null!
        };

        // Act
        var dto = converter.ToDto(room);

        // Assert
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.RoomBook, Is.Not.Null);
        Assert.That(dto.RoomBook, Is.Empty);
        Assert.That(dto.Name, Is.EqualTo("Test Room"));
    }

    [Test]
    public void ToDto_WhenRoomBooksHasItems_ConvertsAllItems()
    {
        // Arrange
        var converter = new RoomDTOConverter();
        var roomId = Guid.NewGuid();
        var bookId1 = Guid.NewGuid();
        var bookId2 = Guid.NewGuid();

        var room = new Room
        {
            Id = new Id(roomId),
            Name = "Test Room",
            RoomBooks = new List<RoomBook>
            {
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 5,
                    Book = new Book { Id = new Id(bookId1), Title = "Book 1" },
                    Room = new Room { Id = new Id(roomId) }
                },
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 3,
                    Book = new Book { Id = new Id(bookId2), Title = "Book 2" },
                    Room = new Room { Id = new Id(roomId) }
                }
            }
        };

        // Act
        var dto = converter.ToDto(room);

        // Assert
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.RoomBook, Has.Count.EqualTo(2));

        var roomBookList = dto.RoomBook.ToList();
        Assert.That(roomBookList[0].BookCount, Is.EqualTo(5));
        Assert.That(roomBookList[1].BookCount, Is.EqualTo(3));
        Assert.That(roomBookList[0].RoomId, Is.EqualTo(roomId));
        Assert.That(roomBookList[1].RoomId, Is.EqualTo(roomId));
        Assert.That(roomBookList[0].BookId, Is.EqualTo(bookId1));
        Assert.That(roomBookList[1].BookId, Is.EqualTo(bookId2));
    }
}