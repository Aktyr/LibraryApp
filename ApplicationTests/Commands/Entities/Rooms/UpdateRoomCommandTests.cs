namespace LibApp.ApplicationTests.Commands.Entities.Rooms;

[TestFixture]
public class UpdateRoomCommandTests
{
    private static RoomValidatorAsync CreateRoomValidator => new();

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
        await roomRepo.AddRangeAsync(rooms.AsEnumerable(), CancellationToken.None);

        var roomToUpdate = rooms[4];
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, new FakeRoomBookSynchronizer(), Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateRoomCommand>.Instance);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = roomToUpdate.Id,
            Name = "Updated Room Name",
            RoomBookDTO = []
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Room updated successfully."));

            var updatedRoom = (await roomRepo.GetAsync(x => x.Id.Value == roomToUpdate.Id.Value)).FirstOrDefault();
            Assert.That(updatedRoom, Is.Not.Null);
            Assert.That(updatedRoom!.Name, Is.EqualTo("Updated Room Name"));
            Assert.That(updatedRoom.RoomBooks, Is.Not.Null);

            // Проверяем, что другие комнаты не изменились
            Assert.That(roomRepo.Entities, Has.Count.EqualTo(10));
        });
    }

    // Простейшая фейковая реализация IRoomBookSynchronizer для тестов
    private class FakeRoomBookSynchronizer : IRoomBookSynchronizer
    {
        public Task SynchronizeAsync(Room room, ICollection<RoomBookDTO> dtoList, CancellationToken cancellationToken = default)
        {
            if (dtoList == null)
            {
                room.RoomBooks = [];
                return Task.CompletedTask;
            }

            var list = dtoList.Select(dto => new RoomBook
            {
                Id = new Id(dto.Id),
                BookCount = dto.BookCount,
                Book = new Book { Id = new Id(dto.BookId) },
                Room = new Room { Id = new Id(dto.RoomId) }
            }).ToList();

            room.RoomBooks = list;
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Execute_UpdateNonExistingRoom_ThrowsException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = (FakeRepository<Room>)unitOfWork.GetRepository<Room>();
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, new FakeRoomBookSynchronizer(), Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateRoomCommand>.Instance);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Room Name",
            RoomBookDTO = []
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
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, new FakeRoomBookSynchronizer(), Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateRoomCommand>.Instance);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Room Name",
            RoomBookDTO = []
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
        await roomRepo.AddRangeAsync([room], CancellationToken.None);

        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, new FakeRoomBookSynchronizer(), Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateRoomCommand>.Instance);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = room.Id,
            Name = "Original Name", // То же имя
            RoomBookDTO = []
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Room updated successfully."));

            var updatedRoom = (await roomRepo.GetAsync(x => x.Id.Value == room.Id.Value)).FirstOrDefault();
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
        await roomRepo.AddRangeAsync([room], CancellationToken.None);

        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, new FakeRoomBookSynchronizer(), Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateRoomCommand>.Instance);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = room.Id,
            Name = "", // Пустое имя - невалидно
            RoomBookDTO = []
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
        await roomRepo.AddRangeAsync([room], CancellationToken.None);

        var updateRoomCommand = new UpdateRoomCommand(unitOfWork, CreateRoomValidator, new FakeRoomBookSynchronizer(), Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateRoomCommand>.Instance);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = roomId,
            Name = "Updated Room",
            RoomBookDTO =
                [
                new(Guid.NewGuid(), roomId.Value, bookId1.Value, 5),
                new(Guid.NewGuid(), roomId.Value, bookId2.Value, 3)
                ]
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedRoom = (await roomRepo.GetAsync(x => x.Id.Value == roomId.Value)).FirstOrDefault();
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
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 1,
                    Book = new Book { Id = new Id(Guid.NewGuid()) },
                    Room = new Room { Id = new Id(Guid.NewGuid()) }
                }
            ]
        };
        await roomRepo.AddRangeAsync([room], CancellationToken.None);

        var unitOfWork2 = new FakeUnitOfWork();
        var roomRepo2 = (FakeRepository<Room>)unitOfWork2.GetRepository<Room>();
        await roomRepo2.AddRangeAsync([room], CancellationToken.None);
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork2, CreateRoomValidator, new FakeRoomBookSynchronizer(), Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateRoomCommand>.Instance);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = room.Id,
            Name = "Updated Room",
            RoomBookDTO = null // null коллекция
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedRoom = (await roomRepo.GetAsync(x => x.Id.Value == room.Id.Value)).FirstOrDefault();
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
                new RoomBook
                {
                    Id = new Id(Guid.NewGuid()),
                    BookCount = 10,
                    Book = new Book { Id = new Id(Guid.NewGuid()) },
                    Room = new Room { Id = roomId }
                }
            ]
        };
        await roomRepo.AddRangeAsync([room], CancellationToken.None);

        var newBookId = new Id(Guid.NewGuid());
        var unitOfWork3 = new FakeUnitOfWork();
        var roomRepo3 = (FakeRepository<Room>)unitOfWork3.GetRepository<Room>();
        await roomRepo3.AddRangeAsync([room], CancellationToken.None);
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork3, CreateRoomValidator, new FakeRoomBookSynchronizer(), Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateRoomCommand>.Instance);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = roomId,
            Name = "Updated Room",
            RoomBookDTO =
            [
                new RoomBookDTO(Guid.NewGuid(), roomId.Value, newBookId.Value, 7)
            ]
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedRoom = (await roomRepo.GetAsync(x => x.Id.Value == roomId.Value)).FirstOrDefault();
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
        await roomRepo.AddRangeAsync([room], CancellationToken.None);

        var unitOfWork4 = new FakeUnitOfWork();
        var roomRepo4 = (FakeRepository<Room>)unitOfWork4.GetRepository<Room>();
        await roomRepo4.AddRangeAsync([room], CancellationToken.None);
        var updateRoomCommand = new UpdateRoomCommand(unitOfWork4, CreateRoomValidator, new FakeRoomBookSynchronizer(), Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateRoomCommand>.Instance);
        var updateRoomRequest = new UpdateRoomRequest
        {
            Id = room.Id,
            Name = "R", // Минимально допустимое имя
            RoomBookDTO = []
        };

        // Act
        var response = await updateRoomCommand.Execute(updateRoomRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedRoom = (await roomRepo.GetAsync(x => x.Id.Value == room.Id.Value)).FirstOrDefault();
            Assert.That(updatedRoom, Is.Not.Null);
            Assert.That(updatedRoom!.Name, Is.EqualTo("R"));
        });
    }

    [Test]
    public async Task Execute_WhenNameAlreadyExists_ThrowsRoomExistsException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = unitOfWork.GetRepository<Room>();
        var room1 = new Room { Id = new Id(Guid.NewGuid()), Name = "Existing" };
        var room2 = new Room { Id = new Id(Guid.NewGuid()), Name = "ToUpdate" };
        await roomRepo.AddRangeAsync([room1, room2], CancellationToken.None);

        var command = new UpdateRoomCommand(unitOfWork, new RoomValidatorAsync(), new FakeRoomBookSynchronizer(), NullLogger<UpdateRoomCommand>.Instance);
        var request = new UpdateRoomRequest { Id = room2.Id, Name = "Existing", RoomBookDTO = [] };

        // Act & Assert
        Assert.ThrowsAsync<RoomExistsException>(() => command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenSynchronizerThrows_RollsBackTransaction()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = unitOfWork.GetRepository<Room>();
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Room" };
        await roomRepo.AddRangeAsync([room], CancellationToken.None);

        var syncMock = new Mock<IRoomBookSynchronizer>();
        syncMock.Setup(s => s.SynchronizeAsync(It.IsAny<Room>(), It.IsAny<ICollection<RoomBookDTO>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Sync error"));

        var command = new UpdateRoomCommand(unitOfWork, new RoomValidatorAsync(), syncMock.Object, NullLogger<UpdateRoomCommand>.Instance);
        var request = new UpdateRoomRequest { Id = room.Id, Name = "NewName", RoomBookDTO = [] };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => command.Execute(request, CancellationToken.None));
    }
    [Test]
    public async Task Execute_WhenSaveFails_RollsBackTransaction()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var roomRepoMock = new Mock<IRepository<Room>>();
        unitOfWorkMock.Setup(u => u.GetRepository<Room>()).Returns(roomRepoMock.Object);

        var roomId = new Id(Guid.NewGuid());
        var room = new Room { Id = roomId, Name = "Room", RoomBooks = [] };
        roomRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Room, bool>>>(), null, null, null, It.IsAny<string[]>()))
                    .ReturnsAsync([room]);

        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new Exception("DB error"));

        var validator = new RoomValidatorAsync();
        var synchronizerMock = new Mock<IRoomBookSynchronizer>();
        var logger = NullLogger<UpdateRoomCommand>.Instance;
        var command = new UpdateRoomCommand(unitOfWorkMock.Object, validator, synchronizerMock.Object, logger);
        UpdateRoomRequest request = new() { Id = roomId, Name = "NewName", RoomBookDTO = [] };

        // Act & Assert
        Assert.ThrowsAsync<Exception>(() => command.Execute(request, CancellationToken.None));

        unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}