namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class EmailValidatorAsyncTests
{
    [Test]
    public async Task ValidateAsync_WithValidEmail_ReturnsIsValid()
    {
        // Arrange
        var validator = new EmailValidatorAsync();
        var email = "user@example.com";

        // Act
        var result = await validator.ValidateAsync(email);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateAsync_WithEmptyEmail_ReturnsError()
    {
        // Arrange
        var validator = new EmailValidatorAsync();
        var email = "";

        // Act
        var result = await validator.ValidateAsync(email);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Email обязателен"));
        });
    }

    [Test]
    public async Task ValidateAsync_WithInvalidEmail_ReturnsError()
    {
        // Arrange
        var validator = new EmailValidatorAsync();
        var email = "not-an-email";

        // Act
        var result = await validator.ValidateAsync(email);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Некорректный формат email"));
        });
    }

    [Test]
    public async Task ValidateAsync_WithEmailWithoutAtSymbol_ReturnsError()
    {
        // Arrange
        var validator = new EmailValidatorAsync();
        var email = "userexample.com";

        // Act
        var result = await validator.ValidateAsync(email);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Некорректный формат email"));
        });
    }

    [Test]
    public async Task ValidateAsync_WithEmailWithoutDomain_ReturnsError()
    {
        // Arrange
        var validator = new EmailValidatorAsync();
        var email = "user@";

        // Act
        var result = await validator.ValidateAsync(email);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Некорректный формат email"));
        });
    }
}