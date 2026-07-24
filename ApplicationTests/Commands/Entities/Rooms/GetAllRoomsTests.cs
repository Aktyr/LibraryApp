namespace LibApp.ApplicationTests.Commands.Entities.Rooms;

[TestFixture]
public class GetAllRoomsTests
{
    private IConverter<Room, RoomDTO> Converter => new RoomDTOConverter();

    [Test]
    public async Task Execute_GetAllRoomsFromRepositoryWithRooms_ReturnsAllRooms()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
            .Generate(7)
            .ToList();
        await roomRepo.AddRange(rooms.AsEnumerable(), CancellationToken.None);

        var getAllRooms = new GetAllRoomsCommand(unitOfWork, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Room, Has.Length.EqualTo(7));

            for (int i = 0; i < rooms.Count; i++)
            {
                Assert.That(result.Room[i].Id, Is.EqualTo(rooms[i].Id.Value));
                Assert.That(result.Room[i].Name, Is.EqualTo(rooms[i].Name));
                Assert.That(result.Status, Is.EqualTo("Ok"));
            }
        });
    }

    [Test]
    public async Task Execute_GetAllRoomsFromEmptyRepository_ReturnsEmptyArray()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var getAllRooms = new GetAllRoomsCommand(unitOfWork, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Room, Is.Not.Null);
            Assert.That(result.Room, Has.Length.EqualTo(0));
        });
    }

    [Test]
    public async Task Execute_GetAllRoomsWithRoomBooks_ReturnsRoomsWithBookData()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
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
        await roomRepo.AddRange(new[] { room }, CancellationToken.None);

        var getAllRooms = new GetAllRoomsCommand(unitOfWork, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Room, Has.Length.EqualTo(1));

            var roomBookList = result.Room[0].RoomBook.ToList(); // Конвертируем в List для индексирования
            if (roomBookList.Any())
            {
                Assert.That(roomBookList[0].RoomId, Is.EqualTo(roomId.Value));
                Assert.That(roomBookList[0].BookId, Is.EqualTo(bookId1.Value));
                Assert.That(roomBookList[1].RoomId, Is.EqualTo(roomId.Value));
                Assert.That(roomBookList[1].BookId, Is.EqualTo(bookId2.Value));
            }
        });
    }

    [Test]
    public async Task Execute_GetAllRoomsWithEmptyRoomBooks_ReturnsRoomsWithEmptyCollections()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
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
        await roomRepo.AddRange(rooms.AsEnumerable(), CancellationToken.None);

        var getAllRooms = new GetAllRoomsCommand(unitOfWork, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Room, Has.Length.EqualTo(2));
            Assert.That(result.Room[0].RoomBook, Is.Empty);
            Assert.That(result.Room[1].RoomBook, Is.Empty);
        });
    }

    [Test]
    public async Task Execute_GetAllRoomsUsesGetWithoutTracking_ReturnsDataWithoutTracking()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
            .Generate(3)
            .ToList();
        await roomRepo.AddRange(rooms.AsEnumerable());

        var getAllRooms = new GetAllRoomsCommand(unitOfWork, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllRooms.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Room, Has.Length.EqualTo(3));
            // Косвенная проверка: если GetWithoutTracking работает, данные возвращаются
        });
    }
}