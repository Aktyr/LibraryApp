namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class RoomBookSynchronizationValidatorTests
{
    private static RoomBookSynchronizationValidator CreateValidator() => new();

    [Test]
    public async Task ValidateAsync_WhenRoomIsNull_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var dtoList = new List<RoomBookDTO>();
        var existing = new List<RoomBook>();

        // Act
        var result = await validator.ValidateAsync(null!, dtoList, existing);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Комната не может быть null"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenDtoListHasNegativeBookCount_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test" };
        var dtoList = new List<RoomBookDTO>
        {
            new(Guid.NewGuid(), room.Id.Value, Guid.NewGuid(), -5)
        };
        var existing = new List<RoomBook>();

        // Act
        var result = await validator.ValidateAsync(room, dtoList, existing);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Количество книг не может быть отрицательным (BookId: " + dtoList[0].BookId + ")"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenDuplicateBookIdsInDto_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test" };
        var bookId = Guid.NewGuid();
        var dtoList = new List<RoomBookDTO>
        {
            new(Guid.NewGuid(), room.Id.Value, bookId, 5),
            new(Guid.NewGuid(), room.Id.Value, bookId, 3)
        };
        var existing = new List<RoomBook>();

        // Act
        var result = await validator.ValidateAsync(room, dtoList, existing);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item($"Книга с Id {bookId} указана несколько раз в списке"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenUpdatingExistingRoomBookWithDifferentBookId_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var roomId = new Id(Guid.NewGuid());
        var bookId1 = Guid.NewGuid();
        var bookId2 = Guid.NewGuid();
        var existingRoomBookId = Guid.NewGuid();

        var existing = new List<RoomBook>
        {
            new() {
                Id = new Id(existingRoomBookId),
                Book = new Book { Id = new Id(bookId1) },
                Room = new Room { Id = roomId },
                BookCount = 10,
                BorrowedCount = 0
            }
        };

        var dtoList = new List<RoomBookDTO>
        {
            new(existingRoomBookId, roomId.Value, bookId2, 10)
        };

        // Act
        var result = await validator.ValidateAsync(new Room { Id = roomId, Name = "Test" }, dtoList, existing);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item($"Нельзя изменить книгу для существующей записи RoomBook (Id: {existingRoomBookId})"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenUpdatingExistingRoomBookWithBookCountLessThanBorrowedCount_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var roomId = new Id(Guid.NewGuid());
        var bookId = Guid.NewGuid();
        var existingRoomBookId = Guid.NewGuid();

        var existing = new List<RoomBook>
        {
            new() {
                Id = new Id(existingRoomBookId),
                Book = new Book { Id = new Id(bookId) },
                Room = new Room { Id = roomId },
                BookCount = 10,
                BorrowedCount = 7
            }
        };

        var dtoList = new List<RoomBookDTO>
        {
            new(existingRoomBookId, roomId.Value, bookId, 5)
        };

        // Act
        var result = await validator.ValidateAsync(new Room { Id = roomId, Name = "Test" }, dtoList, existing);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item($"Нельзя установить общее количество (5) меньше уже выданных (7) для RoomBook Id {existingRoomBookId}"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenAddingNewBookThatAlreadyExistsInRoom_ReturnsError()
    {
        // Arrange
        var validator = CreateValidator();
        var roomId = new Id(Guid.NewGuid());
        var bookId = Guid.NewGuid();

        var existing = new List<RoomBook>
        {
            new() {
                Id = new Id(Guid.NewGuid()),
                Book = new Book { Id = new Id(bookId) },
                Room = new Room { Id = roomId },
                BookCount = 5,
                BorrowedCount = 0
            }
        };

        var dtoList = new List<RoomBookDTO>
        {
            new(Guid.Empty, roomId.Value, bookId, 3)
        };

        // Act
        var result = await validator.ValidateAsync(new Room { Id = roomId, Name = "Test" }, dtoList, existing);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item($"Книга с Id {bookId} уже присутствует в комнате. Используйте существующую запись (укажите Id) для изменения количества."));
        });
    }

    [Test]
    public async Task ValidateAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var validator = CreateValidator();
        var roomId = new Id(Guid.NewGuid());
        var bookId1 = Guid.NewGuid();
        var bookId2 = Guid.NewGuid();

        var existing = new List<RoomBook>
        {
            new() {
                Id = new Id(Guid.NewGuid()),
                Book = new Book { Id = new Id(bookId1) },
                Room = new Room { Id = roomId },
                BookCount = 10,
                BorrowedCount = 3
            }
        };

        var dtoList = new List<RoomBookDTO>
        {
            new(existing[0].Id.Value, roomId.Value, bookId1, 12),
            new(Guid.Empty, roomId.Value, bookId2, 5)
        };

        // Act
        var result = await validator.ValidateAsync(new Room { Id = roomId, Name = "Test" }, dtoList, existing);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }
}