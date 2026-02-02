namespace LibApp.ApplicationTests.Rooms;

[TestFixture]
public class DeleteRoomCommandTests
{
    [Test]
    public async Task Execute_DeleteExistingRoom_DeletesRoom()
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

        var roomToDelete = rooms[5];
        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = roomToDelete.Id };

        // Act
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Room deleted successfully."));
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(9));
            Assert.That((await roomRepo.Get(x => x.Id.Value == roomToDelete.Id.Value)).Any(), Is.False);
        });
    }

    [Test]
    public async Task Execute_DeleteNonExistingRoom_ReturnsError()
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
        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = nonExistingId };

        // Act
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Error"));
            Assert.That(response.Message, Is.EqualTo("Room not found."));
        });
    }

    [Test]
    public async Task Execute_DeleteRoomFromEmptyRepository_ReturnsError()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = new Id(Guid.NewGuid()) };

        // Act
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Error"));
            Assert.That(response.Message, Is.EqualTo("Room not found."));
        });
    }

    [Test]
    public async Task Execute_DeleteLastRoom_RepositoryBecomesEmpty()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var room = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => "Last Room")
            .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
            .Generate();
        await roomRepo.AddRange([room]);

        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = room.Id };

        // Act
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(0));
            Assert.That((await roomRepo.Get()).Any(), Is.False);
        });
    }

    [Test]
    public async Task Execute_DeleteRoomWithRoomBooks_StillDeletesRoom()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Room with Books",
            RoomBooks = new List<RoomBook>
            {
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 5,
                    Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" },
                    Room = new Room { Id = new Id(Guid.NewGuid()) }
                },
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 3,
                    Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2" },
                    Room = new Room { Id = new Id(Guid.NewGuid()) }
                }
            }
        };
        await roomRepo.AddRange([room]);

        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = room.Id };

        // Act
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Room deleted successfully."));
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(0));
        });
    }

    [Test]
    public async Task Execute_DeleteSpecificRoomByName_DeletesCorrectRoom()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.IndexFaker == 7 ? "Target Room" : f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
            .Generate(10)
            .ToList();
        await roomRepo.AddRange(rooms.AsEnumerable());

        var targetRoom = rooms[7];
        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = targetRoom.Id };

        // Act
        var response = await deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That((await roomRepo.Get(x => x.Name == "Target Room")).Any(), Is.False);
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(9));
        });
    }
}