namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class RoomBookValidatorAsyncTests
{
    [Test]
    public async Task ValidateAsync_WithValidData_ReturnsValid()
    {
        // Arrange
        var validator = new RoomBookValidatorAsync();
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 10,
            Room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" },
            Book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book" }
        };

        // Act
        var result = await validator.ValidateAsync(roomBook);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public async Task ValidateAsync_WhenBookCountIsNegative_ReturnsError()
    {
        // Arrange
        var validator = new RoomBookValidatorAsync();
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = -1,
            Room = new Room { Id = new Id(Guid.NewGuid()) },
            Book = new Book { Id = new Id(Guid.NewGuid()) }
        };

        // Act
        var result = await validator.ValidateAsync(roomBook);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Количество книг не может быть отрицательным"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenRoomIsNull_ReturnsError()
    {
        // Arrange
        var validator = new RoomBookValidatorAsync();
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 10,
            Room = null!,
            Book = new Book { Id = new Id(Guid.NewGuid()) }
        };

        // Act
        var result = await validator.ValidateAsync(roomBook);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Комната обязательна"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenBookIsNull_ReturnsError()
    {
        // Arrange
        var validator = new RoomBookValidatorAsync();
        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 10,
            Room = new Room { Id = new Id(Guid.NewGuid()) },
            Book = null!
        };

        // Act
        var result = await validator.ValidateAsync(roomBook);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Книга обязательна"));
        });
    }
}