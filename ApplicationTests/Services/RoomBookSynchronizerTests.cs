namespace LibApp.ApplicationTests.Services;

[TestFixture]
public class RoomBookSynchronizerTests
{
    private RoomBookSynchronizer CreateSynchronizer(
        IUnitOfWork unitOfWork,
        IRoomBookSynchronizationValidator validator = null!)
    {
        validator ??= Mock.Of<IRoomBookSynchronizationValidator>(v =>
            v.ValidateAsync(It.IsAny<Room>(), It.IsAny<ICollection<RoomBookDTO>>(),
                It.IsAny<ICollection<RoomBook>>(), It.IsAny<CancellationToken>())
                == Task.FromResult(new ValidationResponse(true, new List<string>())));

        return new RoomBookSynchronizer(
            unitOfWork,
            NullLogger<RoomBookSynchronizer>.Instance,
            validator);
    }

    [Test]
    public async Task SynchronizeAsync_WhenDtoListIsEmpty_RemovesAllRoomBooks()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = unitOfWork.GetRepository<Room>();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var roomId = new Id(Guid.NewGuid());
        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2" };

        await bookRepo.AddRangeAsync(new[] { book1, book2 }, CancellationToken.None);

        var room = new Room
        {
            Id = roomId,
            Name = "Test Room",
            RoomBooks = new List<RoomBook>() // пустая коллекция, добавим позже
        };

        // Добавляем RoomBooks после создания room
        room.RoomBooks.Add(new RoomBook { Id = new Id(Guid.NewGuid()), Book = book1, Room = room, BookCount = 5 });
        room.RoomBooks.Add(new RoomBook { Id = new Id(Guid.NewGuid()), Book = book2, Room = room, BookCount = 3 });

        await roomRepo.AddRangeAsync(new[] { room }, CancellationToken.None);

        var synchronizer = CreateSynchronizer(unitOfWork);
        var dtoList = new List<RoomBookDTO>(); // пустой список – удалить всё

        // Act
        await synchronizer.SynchronizeAsync(room, dtoList, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            var updatedRoom = (await roomRepo.GetAsync(r => r.Id.Value == roomId.Value)).First();
            Assert.That(updatedRoom.RoomBooks, Is.Empty);
            var allRoomBooks = await roomBookRepo.GetAsync();
            Assert.That(allRoomBooks, Is.Empty);
        });
    }

    [Test]
    public async Task SynchronizeAsync_WhenAddingNewBooks_CreatesRoomBookRecords()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = unitOfWork.GetRepository<Room>();
        var bookRepo = unitOfWork.GetRepository<Book>();

        var roomId = new Id(Guid.NewGuid());
        var existingBook = new Book { Id = new Id(Guid.NewGuid()), Title = "Existing" };
        var newBook = new Book { Id = new Id(Guid.NewGuid()), Title = "New" };

        await bookRepo.AddRangeAsync(new[] { existingBook, newBook }, CancellationToken.None);

        var room = new Room
        {
            Id = roomId,
            Name = "Test Room",
            RoomBooks = new List<RoomBook>()
        };
        room.RoomBooks.Add(new RoomBook { Id = new Id(Guid.NewGuid()), Book = existingBook, Room = room, BookCount = 2 });

        await roomRepo.AddRangeAsync(new[] { room }, CancellationToken.None);

        var synchronizer = CreateSynchronizer(unitOfWork);

        var dtoList = new List<RoomBookDTO>
        {
            new RoomBookDTO(Guid.NewGuid(), roomId.Value, existingBook.Id.Value, 5), // обновляем количество
            new RoomBookDTO(Guid.NewGuid(), roomId.Value, newBook.Id.Value, 3)       // новая запись
        };

        // Act
        await synchronizer.SynchronizeAsync(room, dtoList, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            var updatedRoom = (await roomRepo.GetAsync(r => r.Id.Value == roomId.Value)).First();
            Assert.That(updatedRoom.RoomBooks, Has.Count.EqualTo(2));

            var existingRb = updatedRoom.RoomBooks.First(rb => rb.Book.Id.Value == existingBook.Id.Value);
            Assert.That(existingRb.BookCount, Is.EqualTo(5));

            var newRb = updatedRoom.RoomBooks.First(rb => rb.Book.Id.Value == newBook.Id.Value);
            Assert.That(newRb.BookCount, Is.EqualTo(3));
            Assert.That(newRb.BorrowedCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task SynchronizeAsync_WhenBookNotFound_ThrowsLibValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = unitOfWork.GetRepository<Room>();
        var bookRepo = unitOfWork.GetRepository<Book>();

        var roomId = new Id(Guid.NewGuid());
        var existingBook = new Book { Id = new Id(Guid.NewGuid()), Title = "Existing" };
        await bookRepo.AddRangeAsync(new[] { existingBook }, CancellationToken.None);

        var room = new Room
        {
            Id = roomId,
            Name = "Test Room",
            RoomBooks = new List<RoomBook>()
        };
        room.RoomBooks.Add(new RoomBook { Id = new Id(Guid.NewGuid()), Book = existingBook, Room = room, BookCount = 2 });

        await roomRepo.AddRangeAsync(new[] { room }, CancellationToken.None);

        var synchronizer = CreateSynchronizer(unitOfWork);

        var missingBookId = Guid.NewGuid();
        var dtoList = new List<RoomBookDTO>
        {
            new RoomBookDTO(Guid.NewGuid(), roomId.Value, missingBookId, 1)
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            synchronizer.SynchronizeAsync(room, dtoList, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Contains.Item($"Книга с Id {missingBookId} не найдена"));
    }

    [Test]
    public async Task SynchronizeAsync_WhenValidatorFails_ThrowsLibValidationException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = unitOfWork.GetRepository<Room>();
        var bookRepo = unitOfWork.GetRepository<Book>();

        var roomId = new Id(Guid.NewGuid());
        var book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book" };
        await bookRepo.AddRangeAsync(new[] { book }, CancellationToken.None);

        var room = new Room
        {
            Id = roomId,
            Name = "Test Room",
            RoomBooks = new List<RoomBook>()
        };
        await roomRepo.AddRangeAsync(new[] { room }, CancellationToken.None);

        var validatorMock = new Mock<IRoomBookSynchronizationValidator>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<Room>(), It.IsAny<ICollection<RoomBookDTO>>(),
                It.IsAny<ICollection<RoomBook>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResponse(false, new List<string> { "Validation error" }));

        var synchronizer = new RoomBookSynchronizer(
            unitOfWork,
            NullLogger<RoomBookSynchronizer>.Instance,
            validatorMock.Object);

        var dtoList = new List<RoomBookDTO>
        {
            new RoomBookDTO(Guid.NewGuid(), roomId.Value, book.Id.Value, 1)
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            synchronizer.SynchronizeAsync(room, dtoList, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Contains.Item("Validation error"));
    }

    [Test]
    public async Task SynchronizeAsync_WhenRoomBooksIsNull_LoadsFromRepository()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = unitOfWork.GetRepository<Room>();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var roomId = new Id(Guid.NewGuid());
        var book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book" };
        await bookRepo.AddRangeAsync(new[] { book }, CancellationToken.None);

        var room = new Room { Id = roomId, Name = "Test Room", RoomBooks = null! }; // явно null
        await roomRepo.AddRangeAsync(new[] { room }, CancellationToken.None);

        // Добавляем RoomBook в БД, но room.RoomBooks == null
        var rb = new RoomBook { Id = new Id(Guid.NewGuid()), Book = book, Room = room, BookCount = 2 };
        await roomBookRepo.AddRangeAsync(new[] { rb }, CancellationToken.None);

        var synchronizer = CreateSynchronizer(unitOfWork);
        var dtoList = new List<RoomBookDTO>
    {
        new RoomBookDTO(rb.Id.Value, roomId.Value, book.Id.Value, 5)
    };

        // Act
        await synchronizer.SynchronizeAsync(room, dtoList, CancellationToken.None);

        // Assert
        var updatedRoom = (await roomRepo.GetAsync(r => r.Id.Value == roomId.Value)).First();
        Assert.That(updatedRoom.RoomBooks, Is.Not.Null);
        Assert.That(updatedRoom.RoomBooks, Has.Count.EqualTo(1));
        Assert.That(updatedRoom.RoomBooks.First().BookCount, Is.EqualTo(5));
    }

    [Test]
    public async Task SynchronizeAsync_WhenRemoving_RemovesCorrectRecords()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var roomRepo = unitOfWork.GetRepository<Room>();
        var bookRepo = unitOfWork.GetRepository<Book>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var roomId = new Id(Guid.NewGuid());
        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book1" };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book2" };
        await bookRepo.AddRangeAsync(new[] { book1, book2 }, CancellationToken.None);

        var room = new Room { Id = roomId, Name = "Test Room", RoomBooks = new List<RoomBook>() };
        var rb1 = new RoomBook { Id = new Id(Guid.NewGuid()), Book = book1, Room = room, BookCount = 3 };
        var rb2 = new RoomBook { Id = new Id(Guid.NewGuid()), Book = book2, Room = room, BookCount = 2 };
        room.RoomBooks.Add(rb1);
        room.RoomBooks.Add(rb2);

        await roomRepo.AddRangeAsync(new[] { room }, CancellationToken.None);
        // Добавляем RoomBook в репозиторий, чтобы синхронизатор мог их найти при удалении/обновлении
        await roomBookRepo.AddRangeAsync(new[] { rb1, rb2 }, CancellationToken.None);

        var synchronizer = CreateSynchronizer(unitOfWork);
        var dtoList = new List<RoomBookDTO>
    {
        new RoomBookDTO(rb1.Id.Value, roomId.Value, book1.Id.Value, 5)
    };

        // Act
        await synchronizer.SynchronizeAsync(room, dtoList, CancellationToken.None);

        // Assert
        Assert.That(room.RoomBooks, Has.Count.EqualTo(1));
        Assert.That(room.RoomBooks.First().Id.Value, Is.EqualTo(rb1.Id.Value));

        var allRoomBooks = await roomBookRepo.GetAsync();
        Assert.Multiple(() =>
        {
            Assert.That(allRoomBooks.Count(), Is.EqualTo(1));
            Assert.That(allRoomBooks.First().Id.Value, Is.EqualTo(rb1.Id.Value));
        });
    }
    [Test]
    public async Task SynchronizeAsync_WhenRoomIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var synchronizer = CreateSynchronizer(unitOfWork);
        var dtoList = new List<RoomBookDTO>();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            synchronizer.SynchronizeAsync(null!, dtoList, CancellationToken.None));
    }
}