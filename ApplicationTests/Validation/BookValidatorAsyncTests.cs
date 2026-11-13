using LibApp.Core.Policies;

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

    [Test]
    public async Task ValidateAsync_WhenGenreTooLong_ReturnsError()
    {
        // Arrange
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Valid Title",
            Author = "Valid Author",
            Year = 2000,
            Publisher = "Valid Publisher",
            Genre = new string('A', 101) // > 100
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("Название жанра не может превышать 100 символов"));
        });
    }
    [Test]
    public async Task ValidateAsync_WhenDescriptionAndCoverUrlAreEmpty_ReturnsIsValid()
    {
        // Arrange: пустые строки — валидное состояние (поля необязательны).
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Война и мир",
            Author = "Лев Толстой",
            Year = 1869,
            Publisher = "Русский вестник",
            Description = "",
            Images = [new BookImage { Url = "" }]
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
    public async Task ValidateAsync_WhenDescriptionExceedsMaxLength_ReturnsError()
    {
        // Arrange
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Valid Title",
            Author = "Valid Author",
            Year = 2000,
            Publisher = "Valid Publisher",
            Description = new string('A', BookPolicy.MaxDescriptionLength + 1)
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors,
                Contains.Item($"Описание не может превышать {BookPolicy.MaxDescriptionLength} символов"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenCoverUrlIsNotAbsoluteHttp_ReturnsError()
    {
        // Arrange: относительный URL — не абсолютный, должен отсекаться.
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Valid Title",
            Author = "Valid Author",
            Year = 2000,
            Publisher = "Valid Publisher",
            Images = [ new BookImage { Url = "/images/cover.png" } ]
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("URL изображения должен быть абсолютным http/https-адресом"));
        });
    }

    [Test]
    public async Task ValidateAsync_WhenCoverUrlHasUnsafeScheme_ReturnsError()
    {
        // Arrange: javascript: и data: — XSS-векторы, не должны проходить.
        var validator = new BookValidatorAsync();

        foreach (var unsafeUrl in new[] { "javascript:alert(1)", "data:image/png;base64,AAAA", "ftp://example.com/a.png" })
        {
            var book = new Book
            {
                Id = new Id(Guid.NewGuid()),
                Title = "Valid Title",
                Author = "Valid Author",
                Year = 2000,
                Publisher = "Valid Publisher",
                Images = [new BookImage { Url = unsafeUrl }]
            };

            // Act
            var result = await validator.ValidateAsync(book);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False, $"URL '{unsafeUrl}' должен быть отклонён");
                Assert.That(result.Errors, Contains.Item("URL изображения должен быть абсолютным http/https-адресом"));
            });
        }
    }

    [Test]
    public async Task ValidateAsync_WhenCoverUrlIsValidHttps_ReturnsIsValid()
    {
        // Arrange
        var validator = new BookValidatorAsync();
        var book = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Valid Title",
            Author = "Valid Author",
            Year = 2000,
            Publisher = "Valid Publisher",
            Images = [new BookImage { Url = "https://example.com/cover.png" }]
        };

        // Act
        var result = await validator.ValidateAsync(book);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }
}