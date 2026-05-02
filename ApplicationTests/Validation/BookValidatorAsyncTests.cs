namespace LibApp.ApplicationTests.Validation;

[TestFixture]
public class BookValidatorAsyncTests
{
    [Test]
    public async Task ValidateAsync_WithAllValidFields_ReturnsIsValid()
    {
        // Arrange
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Война и мир",
            Author = "Лев Толстой",
            Year = 1869,
            Publisher = "Русский вестник"
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateAsync_WhenTitleExactlyMaxLength_ReturnsIsValid()
    {
        // Arrange
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = new string('A', 100),
            Author = "Valid Author",
            Year = 2020,
            Publisher = "Valid Publisher"
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateAsync_WhenAuthorExactlyMaxLength_ReturnsIsValid()
    {
        // Arrange
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Valid Title",
            Author = new string('B', 100),
            Year = 2020,
            Publisher = "Valid Publisher"
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateAsync_WhenYearIsFutureUpTo5Years_ReturnsIsValid()
    {
        // Arrange
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Future Book",
            Author = "Author",
            Year = DateTime.Now.Year + 5,
            Publisher = "Publisher"
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task ValidateAsync_WhenYearExceedsFutureLimit_ReturnsError()
    {
        // Arrange
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Too Future Book",
            Author = "Author",
            Year = DateTime.Now.Year + 6,
            Publisher = "Publisher"
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item($"Год должен быть между 0 и {DateTime.Now.Year + 5}"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenYearIsNegative_ReturnsError()
    {
        // Arrange
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Negative Year Book",
            Author = "Author",
            Year = -1,
            Publisher = "Publisher"
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item($"Год должен быть между 0 и {DateTime.Now.Year + 5}"));
        });
    }
}