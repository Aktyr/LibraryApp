namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class UserValidatorAsyncTests
{
    [Test]
    public async Task ValidateAsync_WithAllValidFields_ReturnsIsValid()
    {
        // Arrange
        var validator = new UserValidatorAsync();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "test@test.com"
        };

        // Act
        var result = await validator.ValidateAsync(user);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateAsync_WhenNamesExactlyMaxLength_ReturnsIsValid()
    {
        // Arrange
        var validator = new UserValidatorAsync();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = new string('A', 100),
            FirstName = new string('B', 100),
            MiddleName = new string('C', 100),
            ContactInfo = "test@test.com"
        };

        // Act
        var result = await validator.ValidateAsync(user);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateAsync_WhenLastNameExceedsMaxLength_ReturnsError()
    {
        // Arrange
        var validator = new UserValidatorAsync();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = new string('A', 101),
            FirstName = "Иван",
            ContactInfo = "test@test.com"
        };

        // Act
        var result = await validator.ValidateAsync(user);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Фамилия не может превышать 100 символов"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenFirstNameExceedsMaxLength_ReturnsError()
    {
        // Arrange
        var validator = new UserValidatorAsync();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = new string('B', 101),
            ContactInfo = "test@test.com"
        };

        // Act
        var result = await validator.ValidateAsync(user);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Имя не может превышать 100 символов"));
        });
    }
}