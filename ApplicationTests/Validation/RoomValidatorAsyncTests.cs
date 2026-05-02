namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class RoomValidatorAsyncTests
{
    [Test]
    public async Task ValidateAsync_WithValidName_ReturnsIsValid()
    {
        // Arrange
        var validator = new RoomValidatorAsync();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "Читальный зал"
        };

        // Act
        var result = await validator.ValidateAsync(room);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateAsync_WhenNameIsEmpty_ReturnsError()
    {
        // Arrange
        var validator = new RoomValidatorAsync();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = ""
        };

        // Act
        var result = await validator.ValidateAsync(room);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Название комнаты обязательно"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenNameIsWhitespace_ReturnsError()
    {
        // Arrange
        var validator = new RoomValidatorAsync();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = "   "
        };

        // Act
        var result = await validator.ValidateAsync(room);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Название комнаты обязательно"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenNameExactlyMaxLength_ReturnsIsValid()
    {
        // Arrange
        var validator = new RoomValidatorAsync();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = new string('A', 100)
        };

        // Act
        var result = await validator.ValidateAsync(room);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateAsync_WhenNameExceedsMaxLength_ReturnsError()
    {
        // Arrange
        var validator = new RoomValidatorAsync();
        var room = new Room
        {
            Id = new Id(Guid.NewGuid()),
            Name = new string('A', 101)
        };

        // Act
        var result = await validator.ValidateAsync(room);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Название комнаты не может превышать 100 символов"));
        });
    }
}