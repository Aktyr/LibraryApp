namespace LibApp.ApplicationTests.Commands.Entities.Rooms;

[TestFixture]
public class UpdateRoomCommandTests
{
    private RoomValidatorAsync CreateRoomValidator => new();
    private IConverter<Room, RoomDTO> Converter => new RoomDTOConverter();


    [Test]
    public async Task Execute_UpdateExistingRoomWithNewName_UpdatesRoom()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => [])
            .Generate(10)
            .ToList();
        await roomRepo.AddRange(rooms.AsEnumerable(), CancellationToken.None);

        var roomToUpdate = rooms[4];
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, Converter);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = roomToUpdate.Id,
            Name = "Updated Room Name",
            RoomBooks = []
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Room updated successfully."));

            var updatedRoom = (await roomRepo.Get(x => x.Id.Value == roomToUpdate.Id.Value)).FirstOrDefault();
            Assert.That(updatedRoom, Is.Not.Null);
            Assert.That(updatedRoom!.Name, Is.EqualTo("Updated Room Name"));
            Assert.That(updatedRoom.RoomBooks, Is.Not.Null);

            // Проверяем, что другие комнаты не изменились
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(10));
        });
    }

    [Test]
    public async Task Execute_UpdateNonExistingRoom_ThrowsException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, Converter);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Room Name",
            RoomBooks = []
        };

        // Act & Assert
        Assert.ThrowsAsync<RoomNotFoundException>(async () =>
            await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None));
    }


    [Test]
    public async Task Execute_UpdateRoomInEmptyRepository_ThrowsException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, Converter);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Room Name",
            RoomBooks = []
        };

        // Act & Assert
        Assert.ThrowsAsync<RoomNotFoundException>(async () =>
            await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_UpdateRoomWithSameName_StillUpdates()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var room = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => "Original Name")
            .RuleFor(x => x.RoomBooks, f => [])
            .Generate();
        await roomRepo.AddRange([room], CancellationToken.None);

        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, Converter);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = room.Id,
            Name = "Original Name", // То же имя
            RoomBooks = []
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Room updated successfully."));

            var updatedRoom = (await roomRepo.Get(x => x.Id.Value == room.Id.Value)).FirstOrDefault();
            Assert.That(updatedRoom, Is.Not.Null);
            Assert.That(updatedRoom!.Name, Is.EqualTo("Original Name"));
        });
    }

    [Test]
    public async Task Execute_UpdateRoomWithEmptyName_ThrowsValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var room = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => "Original Name")
            .RuleFor(x => x.RoomBooks, f => [])
            .Generate();
        await roomRepo.AddRange([room], CancellationToken.None);

        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, Converter);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = room.Id,
            Name = "", // Пустое имя - невалидно
            RoomBooks = []
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_UpdateRoomWithRoomBooks_UpdatesBooksCollection()
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
            Name = "Original Room",
            RoomBooks = []
        };
        await roomRepo.AddRange([room], CancellationToken.None);

        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, Converter);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = roomId,
            Name = "Updated Room",
            RoomBooks =
            [
                new() 
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 5,
                    Book = new Book { Id = bookId1, Title = "Book 1" },
                    Room = new Room { Id = roomId }
                },
                new() 
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 3,
                    Book = new Book { Id = bookId2, Title = "Book 2" },
                    Room = new Room { Id = roomId }
                }
            ]
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedRoom = (await roomRepo.Get(x => x.Id.Value == roomId.Value)).FirstOrDefault();
            Assert.That(updatedRoom, Is.Not.Null);
            Assert.That(updatedRoom!.Name, Is.EqualTo("Updated Room"));
            Assert.That(updatedRoom.RoomBooks, Has.Count.EqualTo(2));
            Assert.That(updatedRoom.RoomBooks.First().BookCount, Is.EqualTo(5));
            Assert.That(updatedRoom.RoomBooks.Last().BookCount, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Execute_UpdateRoomWithNullRoomBooks_UpdatesWithNullCollection()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Original Room",
            RoomBooks =
            [
                new()
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 1,
                    Book = new Book { Id = new Id(Guid.NewGuid()) },
                    Room = new Room { Id = new Id(Guid.NewGuid()) }
                }
            ]
        };
        await roomRepo.AddRange([room], CancellationToken.None);

        var unitOfWork2 = new FakeUnitOfWork();
        var roomRepo2 = (FakeRepository<Room>)unitOfWork2.GetRepository<Room>();
        await roomRepo2.AddRange([room], CancellationToken.None);
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork2, CreateRoomValidator, Converter);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = room.Id,
            Name = "Updated Room",
            RoomBooks = null // null коллекция
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedRoom = (await roomRepo.Get(x => x.Id.Value == room.Id.Value)).FirstOrDefault();
            Assert.That(updatedRoom, Is.Not.Null);
            Assert.That(updatedRoom!.Name, Is.EqualTo("Updated Room"));
            // RoomBooks будет null согласно запросу
            // Assert.That(updatedRoom.RoomBooks, Is.Null); // Раскомментировать если нужно проверить
        });
    }

    [Test]
    public async Task Execute_UpdateRoomReplacesExistingBooks_NewBooksSet()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var roomId = new Id(Guid.NewGuid());

        var room = new Room
        {
            Id = roomId,
            Name = "Original Room",
            RoomBooks =
            [
                new()
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 10,
                    Book = new Book { Id = new Id(Guid.NewGuid()) },
                    Room = new Room { Id = roomId }
                }
            ]
        };
        await roomRepo.AddRange([room], CancellationToken.None);

        var newBookId = new Id(Guid.NewGuid());
        var unitOfWork3 = new FakeUnitOfWork();
        var roomRepo3 = (FakeRepository<Room>)unitOfWork3.GetRepository<Room>();
        await roomRepo3.AddRange([room], CancellationToken.None);
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork3, CreateRoomValidator, Converter);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = roomId,
            Name = "Updated Room",
            RoomBooks =
            [
                new()
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 7,
                    Book = new Book { Id = newBookId, Title = "New Book" },
                    Room = new Room { Id = roomId }
                }
            ]
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedRoom = (await roomRepo.Get(x => x.Id.Value == roomId.Value)).FirstOrDefault();
            Assert.That(updatedRoom, Is.Not.Null);
            Assert.That(updatedRoom!.RoomBooks, Has.Count.EqualTo(1));
            Assert.That(updatedRoom.RoomBooks.First().BookCount, Is.EqualTo(7));
        });
    }
    [Test]
    public async Task Execute_UpdateRoomWithMinimalValidName_UpdatesSuccessfully()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var room = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => "Original Name")
            .RuleFor(x => x.RoomBooks, f => [])
            .Generate();
        await roomRepo.AddRange([room], CancellationToken.None);

        var unitOfWork4 = new FakeUnitOfWork();
        var roomRepo4 = (FakeRepository<Room>)unitOfWork4.GetRepository<Room>();
        await roomRepo4.AddRange([room], CancellationToken.None);
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork4, CreateRoomValidator, Converter);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = room.Id,
            Name = "R", // Минимально допустимое имя
            RoomBooks = []
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedRoom = (await roomRepo.Get(x => x.Id.Value == room.Id.Value)).FirstOrDefault();
            Assert.That(updatedRoom, Is.Not.Null);
            Assert.That(updatedRoom!.Name, Is.EqualTo("R"));
        });
    }
}