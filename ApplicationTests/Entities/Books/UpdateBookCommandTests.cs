using LibApp.Application.Validation.Entities;
using LibApp.Core.DTO.Entities;
using LibApp.Core.Exceptions.Entities;

namespace LibApp.ApplicationTests.Entities.Books;

[TestFixture]
public class UpdateBookCommandTests
{
    private BookValidatorAsync CreateBookValidator => new();
    private IConverter<Book, BookDTO> Converter => new BookDTOConverter();


    [Test]
    public async Task Execute_UpdateExistingBookWithValidData_UpdatesBook()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var books = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => f.Lorem.Sentence(3))
            .RuleFor(x => x.Author, f => f.Name.FullName())
            .RuleFor(x => x.Year, f => f.Random.Int(1900, 2024))
            .RuleFor(x => x.Publisher, f => f.Company.CompanyName())
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate(10)
            .ToList();
        await bookRepo.AddRange(books.AsEnumerable());

        var bookToUpdate = books[4];
        var updateBookCommand = new UpdateBookCommand(bookRepo, CreateBookValidator, Converter);
        var updateBookRequest = new UpdateBookRequest
        {
            Id = bookToUpdate.Id,
            Title = "Updated Title",
            Author = "Updated Author",
            Year = 2023,
            Publisher = "Updated Publisher"
        };

        // Act
        var response = await updateBookCommand.Execute(updateBookRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Book updated successfully."));

            var updatedBook = (await bookRepo.Get(x => x.Id.Value == bookToUpdate.Id.Value)).FirstOrDefault();
            Assert.That(updatedBook, Is.Not.Null);
            Assert.That(updatedBook!.Title, Is.EqualTo("Updated Title"));
            Assert.That(updatedBook.Author, Is.EqualTo("Updated Author"));
            Assert.That(updatedBook.Year, Is.EqualTo(2023));
            Assert.That(updatedBook.Publisher, Is.EqualTo("Updated Publisher"));

            // Проверяем, что другие книги не изменились
            Assert.That(bookRepo.Entities, Has.Count.EqualTo(10));
        });
    }

    [Test]
    public async Task Execute_UpdateNonExistingBook_ThrowsBookNotFoundException()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();

        // Создаем 10 тестовых книг
        await bookRepo.AddRange(new Bogus.Faker<Book>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.Title, f => f.Lorem.Sentence(3))
                                   .RuleFor(x => x.Author, f => f.Name.FullName())
                                   .RuleFor(x => x.Year, f => f.Random.Int(1900, 2024))
                                   .RuleFor(x => x.Publisher, f => f.Company.CompanyName())
                                   .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
                                   .Generate(10)
                                   .AsEnumerable());
        var updateBookCommand = new UpdateBookCommand(bookRepo, CreateBookValidator, Converter);
        var updateBookRequest = new UpdateBookRequest
        {
            Id = new Id(Guid.NewGuid()), // ID, которого нет в репозитории
            Title = "Title",
            Author = "Author",
            Year = 2023,
            Publisher = "Publisher"
        };

        // Act & Assert
        Assert.ThrowsAsync<BookNotFoundException>(async () =>
            await updateBookCommand.Execute(updateBookRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_UpdateBookInEmptyRepository_ThrowsBookNotFoundException()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        // Не добавляем книги - репозиторий пустой

        var updateBookCommand = new UpdateBookCommand(bookRepo, CreateBookValidator, Converter);
        var updateBookRequest = new UpdateBookRequest
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Title",
            Author = "Author",
            Year = 2023,
            Publisher = "Publisher"
        };

        // Act & Assert
        Assert.ThrowsAsync<BookNotFoundException>(async () =>
            await updateBookCommand.Execute(updateBookRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_UpdateBookWithSameData_StillUpdates()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var book = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => "Original Title")
            .RuleFor(x => x.Author, f => "Original Author")
            .RuleFor(x => x.Year, f => 2000)
            .RuleFor(x => x.Publisher, f => "Original Publisher")
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate();
        await bookRepo.AddRange([book]);

        var updateBookCommand = new UpdateBookCommand(bookRepo, CreateBookValidator, Converter);
        var updateBookRequest = new UpdateBookRequest
        {
            Id = book.Id,
            Title = "Original Title", // Те же данные
            Author = "Original Author",
            Year = 2000,
            Publisher = "Original Publisher"
        };

        // Act
        var response = await updateBookCommand.Execute(updateBookRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Book updated successfully."));

            var updatedBook = (await bookRepo.Get(x => x.Id.Value == book.Id.Value)).FirstOrDefault();
            Assert.That(updatedBook, Is.Not.Null);
            Assert.That(updatedBook!.Title, Is.EqualTo("Original Title"));
        });
    }

    [Test]
    public async Task Execute_UpdateBookWithEmptyFields_ThrowsValidationException()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var book = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => "Original Title")
            .RuleFor(x => x.Author, f => "Original Author")
            .RuleFor(x => x.Year, f => 2000)
            .RuleFor(x => x.Publisher, f => "Original Publisher")
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate();
        await bookRepo.AddRange([book]);

        var updateBookCommand = new UpdateBookCommand(bookRepo, CreateBookValidator, Converter);
        var updateBookRequest = new UpdateBookRequest
        {
            Id = book.Id,
            Title = "",
            Author = "",
            Year = 0,
            Publisher = ""
        };

        // Act & Assert
        Assert.ThrowsAsync<ValidationException>(async () =>
            await updateBookCommand.Execute(updateBookRequest, CancellationToken.None));
    }


    [Test]
    public async Task Execute_UpdateBookWithInvalidYear_ThrowsValidationException()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var book = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => "Original Title")
            .RuleFor(x => x.Author, f => "Original Author")
            .RuleFor(x => x.Year, f => 2000)
            .RuleFor(x => x.Publisher, f => "Original Publisher")
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate();
        await bookRepo.AddRange([book]);

        var updateBookCommand = new UpdateBookCommand(bookRepo, CreateBookValidator, Converter);
        var updateBookRequest = new UpdateBookRequest
        {
            Id = book.Id,
            Title = "Updated Title",
            Author = "Updated Author",
            Year = -100, // Невалидный год
            Publisher = "Updated Publisher"
        };

        // Act & Assert
        Assert.ThrowsAsync<ValidationException>(async () =>
            await updateBookCommand.Execute(updateBookRequest, CancellationToken.None));
    }
    [Test]
    public async Task Execute_UpdateBookWithMinimalValidData_UpdatesSuccessfully()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var book = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => "Original Title")
            .RuleFor(x => x.Author, f => "Original Author")
            .RuleFor(x => x.Year, f => 2000)
            .RuleFor(x => x.Publisher, f => "Original Publisher")
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate();
        await bookRepo.AddRange([book]);

        var updateBookCommand = new UpdateBookCommand(bookRepo, CreateBookValidator, Converter);
        var updateBookRequest = new UpdateBookRequest
        {
            Id = book.Id,
            Title = "T",
            Author = "A",
            Year = 0,
            Publisher = "P"
        };

        // Act
        var response = await updateBookCommand.Execute(updateBookRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));

            var updatedBook = (await bookRepo.Get(x => x.Id.Value == book.Id.Value)).FirstOrDefault();
            Assert.That(updatedBook, Is.Not.Null);
            Assert.That(updatedBook!.Title, Is.EqualTo("T"));
            Assert.That(updatedBook.Author, Is.EqualTo("A"));
            Assert.That(updatedBook.Year, Is.EqualTo(0));
            Assert.That(updatedBook.Publisher, Is.EqualTo("P"));
        });
    }

}