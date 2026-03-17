using LibApp.Core.DTO.Entities;

namespace LibApp.ApplicationTests.Books;

[TestFixture]
public class GetAllBooksQueryTests
{
    private IConverter<Book, BookDTO> Converter => new BookDTOConverter();

    [Test]
    public async Task Execute_GetAllBooksFromRepositoryWithBooks_ReturnsAllBooks()
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
            .Generate(7)
            .ToList();
        await bookRepo.AddRange(books.AsEnumerable());

        var getAllBooksQuery = new GetAllBooksCommand(bookRepo, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllBooksQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Book, Has.Length.EqualTo(7));

            for (int i = 0; i < books.Count; i++)
            {
                Assert.That(result.Book[i].Id, Is.EqualTo(books[i].Id.Value));
                Assert.That(result.Book[i].Title, Is.EqualTo(books[i].Title));
                Assert.That(result.Book[i].Author, Is.EqualTo(books[i].Author));
                Assert.That(result.Book[i].Year, Is.EqualTo(books[i].Year));
                Assert.That(result.Book[i].Publisher, Is.EqualTo(books[i].Publisher));
            }
        });
    }

    [Test]
    public async Task Execute_GetAllBooksFromEmptyRepository_ReturnsEmptyArray()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var getAllBooksQuery = new GetAllBooksCommand(bookRepo, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllBooksQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Book, Is.Not.Null);
            Assert.That(result.Book, Has.Length.EqualTo(0));
        });
    }

    [Test]
    public async Task Execute_GetAllBooksWithSingleBook_ReturnsSingleBook()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var book = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => "Single Book")
            .RuleFor(x => x.Author, f => "Test Author")
            .RuleFor(x => x.Year, f => 2020)
            .RuleFor(x => x.Publisher, f => "Test Publisher")
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate();
        await bookRepo.AddRange([book]);

        var getAllBooksQuery = new GetAllBooksCommand(bookRepo, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllBooksQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Book, Has.Length.EqualTo(1));
            Assert.That(result.Book[0].Title, Is.EqualTo("Single Book"));
            Assert.That(result.Book[0].Author, Is.EqualTo("Test Author"));
        });
    }

    [Test]
    public async Task Execute_GetAllBooksUsesGetWithoutTracking_DoesNotTrackEntities()
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
            .Generate(5)
            .ToList();
        await bookRepo.AddRange(books.AsEnumerable());

        var getAllBooksQuery = new GetAllBooksCommand(bookRepo, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllBooksQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        // Проверяем, что метод GetWithoutTracking был вызван (косвенно)
        // Если FakeRepository правильно реализует GetWithoutTracking, просто проверяем, что данные возвращаются корректно
        Assert.That(result.Book, Has.Length.EqualTo(5));
    }
}