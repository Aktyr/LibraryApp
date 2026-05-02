namespace LibApp.ApplicationTests.Responses;

[TestFixture]
public class ValidationResponseTests
{
    [Test]
    public void ParameterlessConstructor_InitializesWithFalseAndEmptyList()
    {
        // Act
        var response = new ValidationResponse();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.IsValid, Is.False);
            Assert.That(response.Errors, Is.Not.Null);
            Assert.That(response.Errors, Is.Empty);
        });
    }

    [Test]
    public void ParameterizedConstructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2" };
        var isValid = true;

        // Act
        var response = new ValidationResponse(isValid, errors);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.IsValid, Is.EqualTo(isValid));
            Assert.That(response.Errors, Is.SameAs(errors));
        });
    }
}