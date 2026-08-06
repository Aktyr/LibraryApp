namespace LibApp.ApplicationTests.Commands.Entities.Rooms;

[TestFixture]
public class DeleteRoomCommandTests
{
    [Test]
    public async Task Execute_DeleteExistingRoom_DeletesRoom()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => null!) // Должно быть null, чтобы команда не бросала исключение
            .Generate(10)
            .ToList();
        await roomRepo.AddRangeAsync(rooms.AsEnumerable(), CancellationToken.None);

        var roomToDelete = rooms[5];
        var deleteRoomCommand = new DeleteRoomCommand(unitOfWork);
        var deleteRoomRequest = new DeleteRoomRequest { Id = roomToDelete.Id };

        // Act
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Room deleted successfully."));
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(9));
            Assert.That((await roomRepo.GetAsync(x => x.Id.Value == roomToDelete.Id.Value)).Any(), Is.False);
        });
    }

    [Test]
    public async Task Execute_DeleteNonExistingRoom_ThrowsException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => null!) // Должно быть null
            .Generate(10)
            .ToList();
        await roomRepo.AddRangeAsync(rooms.AsEnumerable(), CancellationToken.None);

        var nonExistingId = new Id(Guid.NewGuid());
        var deleteRoomCommand = new DeleteRoomCommand(unitOfWork);
        var deleteRoomRequest = new DeleteRoomRequest { Id = nonExistingId };

        // Act & Assert
        Assert.ThrowsAsync<RoomNotFoundException>(() =>
            deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteRoomFromEmptyRepository_ThrowsException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var deleteRoomCommand = new DeleteRoomCommand(unitOfWork);
        var deleteRoomRequest = new DeleteRoomRequest { Id = new Id(Guid.NewGuid()) };

        // Act & Assert
        Assert.ThrowsAsync<RoomNotFoundException>(() =>
            deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteLastRoom_RepositoryBecomesEmpty()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var room = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => "Last Room")
            .RuleFor(x => x.RoomBooks, f => null!) // Должно быть null
            .Generate();
        await roomRepo.AddRangeAsync(new[] { room }, CancellationToken.None);

        var deleteRoomCommand = new DeleteRoomCommand(unitOfWork);
        var deleteRoomRequest = new DeleteRoomRequest { Id = room.Id };

        // Act
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(0));
            Assert.That((await roomRepo.GetAsync()).Any(), Is.False);
        });
    }

    [Test]
    public async Task Execute_DeleteRoomWithRoomBooks_ThrowsException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Room with Books",
            RoomBooks = new List<RoomBook> // Не null и не пустой список
            {
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 5,
                    Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" },
                    Room = new Room { Id = new Id(Guid.NewGuid()) }
                }
            }
        };
        await roomRepo.AddRangeAsync(new[] { room }, CancellationToken.None);

        var deleteRoomCommand = new DeleteRoomCommand(unitOfWork);
        var deleteRoomRequest = new DeleteRoomRequest { Id = room.Id };

        // Act & Assert
        Assert.ThrowsAsync<RoomDeletionException>(() =>
            deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteSpecificRoomByName_DeletesCorrectRoom()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.IndexFaker == 7 ? "Target Room" : f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => null!) // Должно быть null
            .Generate(10)
            .ToList();
        await roomRepo.AddRangeAsync(rooms.AsEnumerable(), CancellationToken.None);

        var targetRoom = rooms[7];
        var deleteRoomCommand = new DeleteRoomCommand(unitOfWork);
        var deleteRoomRequest = new DeleteRoomRequest { Id = targetRoom.Id };

        // Act
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That((await roomRepo.GetAsync(x => x.Name == "Target Room")).Any(), Is.False);
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(9));
        });
    }

    [Test]
    public async Task Execute_DeleteRoomWithEmptyRoomBooksList_DeletesRoom()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Room with Empty Books",
            RoomBooks = new List<RoomBook>() // Пустой список, но не null
        };
        await roomRepo.AddRangeAsync(new[] { room }, CancellationToken.None);

        var deleteRoomCommand = new DeleteRoomCommand(unitOfWork);
        var deleteRoomRequest = new DeleteRoomRequest { Id = room.Id };

        // Act & Assert
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(roomRepo.Entities, Is.Empty);
        });
    }
}