namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class RegisterValidatorAsyncTests
{
    [Test]
    public async Task ValidateAsync_WithAllValidFields_ReturnsIsValid()
    {
        // Arrange
        var validator = new RegisterValidatorAsync();
        var request = new RegisterRequest
        {
            Email = "valid@email.com",
            Password = "ValidPassword123",
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "+7-999-123-45-67"
        };

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }
}