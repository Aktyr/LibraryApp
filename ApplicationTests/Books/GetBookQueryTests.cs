using LibApp.Application.Entities.Books.Read;

namespace LibApp.ApplicationTests.Books;

[TestFixture]
public class GetBookQueryTests
{
    [Test]
    public async Task Execute_GetExistingBook_ReturnsBookResponse()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var books = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => f.Lorem.Sentence(3))
            .RuleFor(x => x.Author, f => f.Name.FullName())
            .RuleFor(x => x.Year, f => f.Random.Int(1900, 2024))
            .RuleFor(x => x.Publisher, f => f.Company.CompanyName())
            .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
            .Generate(10)
            .ToList();
        await bookRepo.AddRange(books.AsEnumerable());

        var targetBook = books[3];
        var getBookQuery = new GetBookQuery(bookRepo);
        var getBookRequest = new GetBookRequest { Id = targetBook.Id };

        // Act
        var result = await getBookQuery.Execute(getBookRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Books[0].Id, Is.EqualTo(targetBook.Id.Value));
            Assert.That(result.Books[0].Title, Is.EqualTo(targetBook.Title));
            Assert.That(result.Books[0].Author, Is.EqualTo(targetBook.Author));
            Assert.That(result.Books[0].Year, Is.EqualTo(targetBook.Year));
            Assert.That(result.Books[0].Publisher, Is.EqualTo(targetBook.Publisher));
        });
    }

    [Test]
    public async Task Execute_GetNonExistingBook_ReturnsNull()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        await bookRepo.AddRange(new Bogus.Faker<Book>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.Title, f => f.Lorem.Sentence(3))
                                   .RuleFor(x => x.Author, f => f.Name.FullName())
                                   .RuleFor(x => x.Year, f => f.Random.Int(1900, 2024))
                                   .RuleFor(x => x.Publisher, f => f.Company.CompanyName())
                                   .RuleFor(x => x.RoomBooks, f => new List<RoomBook>())
                                   .Generate(10)
                                   .AsEnumerable());

        var nonExistingId = new Id(Guid.NewGuid());
        var getBookQuery = new GetBookQuery(bookRepo);
        var getBookRequest = new GetBookRequest { Id = nonExistingId };

        // Act
        var result = await getBookQuery.Execute(getBookRequest, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Execute_GetBookFromEmptyRepository_ReturnsNull()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var getBookQuery = new GetBookQuery(bookRepo);
        var getBookRequest = new GetBookRequest { Id = new Id(Guid.NewGuid()) };

        // Act
        var result = await getBookQuery.Execute(getBookRequest, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Null);
    }
}
