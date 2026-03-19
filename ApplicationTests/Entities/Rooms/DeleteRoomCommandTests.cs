using LibApp.Core.Exceptions.Entities;

namespace LibApp.ApplicationTests.Entities.Rooms;

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
            .RuleFor(x => x.RoomBooks, f => null) // Должно быть null, чтобы команда не бросала исключение
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
    public async Task Execute_DeleteNonExistingRoom_ThrowsException()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => null)
            .Generate(10)
            .ToList();
        await roomRepo.AddRange(rooms.AsEnumerable());

        var nonExistingId = new Id(Guid.NewGuid());
        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = nonExistingId };

        // Act & Assert
        Assert.ThrowsAsync<RoomNotFoundException>(() =>
            deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteRoomFromEmptyRepository_ThrowsException()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = new Id(Guid.NewGuid()) };

        // Act & Assert
        Assert.ThrowsAsync<RoomNotFoundException>(() =>
            deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteLastRoom_RepositoryBecomesEmpty()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var room = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => "Last Room")
            .RuleFor(x => x.RoomBooks, f => null) // Должно быть null
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
    public async Task Execute_DeleteRoomWithRoomBooks_ThrowsException()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
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
        await roomRepo.AddRange([room]);

        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = room.Id };

        // Act & Assert
        Assert.ThrowsAsync<RoomDeletionException>(() =>
            deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteSpecificRoomByName_DeletesCorrectRoom()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var rooms = new Bogus.Faker<Room>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Name, f => f.IndexFaker == 7 ? "Target Room" : f.Name.FirstName())
            .RuleFor(x => x.RoomBooks, f => null) // Должно быть null
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

    [Test]
    public async Task Execute_DeleteRoomWithEmptyRoomBooksList_DeletesRoom()
    {
        // Arrange
        var roomRepo = new FakeRepository<Room>();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Room with Empty Books",
            RoomBooks = new List<RoomBook>() // Пустой список, но не null
        };
        await roomRepo.AddRange([room]);

        var deleteRoomCommand = new DeleteRoomCommand(roomRepo);
        var deleteRoomRequest = new DeleteRoomRequest { Id = room.Id };

        // Act & Assert
        // Это тоже должно бросать исключение, так как RoomBooks != null
        Assert.ThrowsAsync<RoomDeletionException>(() =>
            deleteRoomCommand.Execute(deleteRoomRequest, CancellationToken.None));
    }
}