namespace LibApp.ApplicationTests.Rooms;

[TestFixture]
public class GetRoomQueryTests
{
    [Test]
    public async Task Execute_GetExistingRoom_ReturnsRoomResponse()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
            .Generate(10)
            .ToList();
        await roomRepo.AddRange(rooms.AsEnumerable());

        var targetRoom = rooms[3];
        var getRoomQuery = new GetRoomQuery(roomRepo);
        var getRoomRequest = new GetRoomRequest { Id = targetRoom.Id };

        // Act
        var result = await getRoomQuery.Execute(getRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Room.Id, Is.EqualTo(targetRoom.Id.Value));
            Assert.That(result.Room.Name, Is.EqualTo(targetRoom.Name));
            Assert.That(result.Status, Is.EqualTo("Ok"));
        });
    }

    [Test]
    public async Task Execute_GetNonExistingRoom_ThrowsException()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        await roomRepo.AddRange(new Bogus.Faker<Room>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.Name, f => f.Name.FirstName())
                                   .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
                                   .Generate(10)
                                   .AsEnumerable());

        var nonExistingId = new Id(Guid.NewGuid());
        var getRoomQuery = new GetRoomQuery(roomRepo);
        var getRoomRequest = new GetRoomRequest { Id = nonExistingId };

        // Act & Assert
        Assert.ThrowsAsync<RoomNotFoundException>(() =>
            getRoomQuery.Execute(getRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_GetRoomFromEmptyRepository_ThrowsException()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var getRoomQuery = new GetRoomQuery(roomRepo);
        var getRoomRequest = new GetRoomRequest { Id = new Id(Guid.NewGuid()) };

        // Act & Assert
        Assert.ThrowsAsync<RoomNotFoundException>(() =>
            getRoomQuery.Execute(getRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_GetRoomWithBooks_ReturnsRoomWithBookDTOs()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var roomId = new Id(Guid.NewGuid());
        var bookId1 = new Id(Guid.NewGuid());
        var bookId2 = new Id(Guid.NewGuid());

        var room = new Room
        {
            Id = roomId,
            Name = "Library Room",
            RoomBooks = new List<RoomBook>
            {
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 10,
                    Book = new Book { Id = bookId1, Title = "Book 1" },
                    Room = new Room { Id = roomId, Name = "Library Room" }
                },
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 15,
                    Book = new Book { Id = bookId2, Title = "Book 2" },
                    Room = new Room { Id = roomId, Name = "Library Room" }
                }
            }
        };
        await roomRepo.AddRange([room]);

        var getRoomQuery = new GetRoomQuery(roomRepo);
        var getRoomRequest = new GetRoomRequest { Id = roomId };

        // Act
        var result = await getRoomQuery.Execute(getRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Room.RoomBooks.Count, Is.EqualTo(2));

            var firstBook = result.Room.RoomBooks.First();
            var lastBook = result.Room.RoomBooks.Last();

            Assert.That(firstBook.BookCount, Is.EqualTo(10));
            Assert.That(lastBook.BookCount, Is.EqualTo(15));
            Assert.That(firstBook.RoomId, Is.EqualTo(roomId.Value));
            Assert.That(lastBook.RoomId, Is.EqualTo(roomId.Value));
        });
    }

    [Test]
    public async Task Execute_GetRoomWithBookWithoutRoomNavigation_ReturnsRoomWithBookDTOs()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var roomId = new Id(Guid.NewGuid());
        var bookId = new Id(Guid.NewGuid());

        var room = new Room
        {
            Id = roomId,
            Name = "Test Room",
            RoomBooks = new List<RoomBook>
        {
            new RoomBook
            {
                Id = new Id(Guid.NewGuid()),
                BookCount = 5,
                Book = new Book { Id = bookId, Title = "Test Book" }
                // Room не устанавливаем - это может быть null
            }
        }
        };
        await roomRepo.AddRange([room]);

        var getRoomQuery = new GetRoomQuery(roomRepo);
        var getRoomRequest = new GetRoomRequest { Id = roomId };

        // Act
        var result = await getRoomQuery.Execute(getRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Room.RoomBooks.Count, Is.EqualTo(1));
            Assert.That(result.Room.RoomBooks.First().BookCount, Is.EqualTo(5));
            // RoomId может быть Guid.Empty если rb.Room == null
            Assert.That(result.Room.RoomBooks.First().RoomId, Is.EqualTo(Guid.Empty));
        });
    }


    [Test]
    public async Task Execute_GetRoomWithoutBooks_ReturnsEmptyRoomBooksCollection()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Empty Room",
            RoomBooks = new List<RoomBook>()
        };
        await roomRepo.AddRange([room]);

        var getRoomQuery = new GetRoomQuery(roomRepo);
        var getRoomRequest = new GetRoomRequest { Id = room.Id };

        // Act
        var result = await getRoomQuery.Execute(getRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Room.RoomBooks, Is.Not.Null);
            Assert.That(result.Room.RoomBooks, Is.Empty);
        });
    }
}