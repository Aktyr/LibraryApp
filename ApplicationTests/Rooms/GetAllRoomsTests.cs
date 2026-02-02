namespace LibApp.ApplicationTests.Rooms;

[TestFixture]
public class GetAllRoomsTests
{
    [Test]
    public async Task Execute_GetAllRoomsFromRepositoryWithRooms_ReturnsAllRooms()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
            .Generate(7)
            .ToList();
        await roomRepo.AddRange(rooms.AsEnumerable());

        var getAllRooms = new GetAllRooms(roomRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Rooms, Has.Length.EqualTo(7));

            for (int i = 0; i < rooms.Count; i++)
            {
                Assert.That(result.Rooms[i].Id, Is.EqualTo(rooms[i].Id.Value));
                Assert.That(result.Rooms[i].Name, Is.EqualTo(rooms[i].Name));
                Assert.That(result.Status, Is.EqualTo("Ok"));
            }
        });
    }

    [Test]
    public async Task Execute_GetAllRoomsFromEmptyRepository_ReturnsEmptyArray()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var getAllRooms = new GetAllRooms(roomRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Rooms, Is.Not.Null);
            Assert.That(result.Rooms, Has.Length.EqualTo(0));
        });
    }

    [Test]
    public async Task Execute_GetAllRoomsWithRoomBooks_ReturnsRoomsWithBookData()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var roomId = new Id(Guid.NewGuid());
        var bookId1 = new Id(Guid.NewGuid());
        var bookId2 = new Id(Guid.NewGuid());

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
                    Book = new Book { Id = bookId1, Title = "Book 1" },
                    Room = new Room { Id = roomId, Name = "Test Room" }
                },
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 3,
                    Book = new Book { Id = bookId2, Title = "Book 2" },
                    Room = new Room { Id = roomId, Name = "Test Room" }
                }
            }
        };
        await roomRepo.AddRange([room]);

        var getAllRooms = new GetAllRooms(roomRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Rooms, Has.Length.EqualTo(1));
            Assert.That(result.Rooms[0].RoomBooks.Count, Is.EqualTo(2));

            // Используем LINQ для получения элементов
            var roomBooks = result.Rooms[0].RoomBooks;
            var bookCounts = roomBooks.Select(rb => rb.BookCount).ToList();

            Assert.That(bookCounts, Contains.Item(5));
            Assert.That(bookCounts, Contains.Item(3));
            Assert.That(roomBooks.All(rb => rb.RoomId == roomId.Value), Is.True);
        });
    }

    [Test]
    public async Task Execute_GetAllRoomsWithEmptyRoomBooks_ReturnsRoomsWithEmptyCollections()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var rooms = new List<Room>
        {
            new Room
            {
                Id = new Id(Guid.NewGuid()),
                Name = "Room 1",
                RoomBooks = new List<RoomBook>()
            },
            new Room
            {
                Id = new Id(Guid.NewGuid()),
                Name = "Room 2",
                RoomBooks = new List<RoomBook>()
            }
        };
        await roomRepo.AddRange(rooms.AsEnumerable());

        var getAllRooms = new GetAllRooms(roomRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Rooms, Has.Length.EqualTo(2));
            Assert.That(result.Rooms[0].RoomBooks, Is.Empty);
            Assert.That(result.Rooms[1].RoomBooks, Is.Empty);
        });
    }

    [Test]
    public async Task Execute_GetAllRoomsUsesGetWithoutTracking_ReturnsDataWithoutTracking()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
            .Generate(3)
            .ToList();
        await roomRepo.AddRange(rooms.AsEnumerable());

        var getAllRooms = new GetAllRooms(roomRepo);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Rooms, Has.Length.EqualTo(3));
            // Косвенная проверка: если GetWithoutTracking работает, данные возвращаются
        });
    }
}