namespace LibApp.ApplicationTests.Commands.Entities.Books;

[TestFixture]
public class GetBookQueryTests
{
    private static IConverter<Book, BookDTO> Converter => new BookDTOConverter();

    [Test]
    public async Task Execute_GetExistingBook_ReturnsBookResponse()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = (FakeRepository<Book>)unitOfWork.GetRepository<Book>();
        var books = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => f.Lorem.Sentence(3))
            .RuleFor(x => x.Author, f => f.Name.FullName())
            .RuleFor(x => x.Year, f => f.Random.Int(1900, 2024))
            .RuleFor(x => x.Publisher, f => f.Company.CompanyName())
            .RuleFor(x => x.RoomBook, f => [])
            .Generate(10)
            .ToList();
        await bookRepo.AddRangeAsync(books.AsEnumerable(), CancellationToken.None);

        var targetBook = books[3];
        var getBookQuery = new GetBookCommand(unitOfWork, Converter);
        var getBookRequest = new GetBookRequest { Id = targetBook.Id };

        // Act
        var result = await getBookQuery.Execute(getBookRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Book[0].Id, Is.EqualTo(targetBook.Id.Value));
            Assert.That(result.Book[0].Title, Is.EqualTo(targetBook.Title));
            Assert.That(result.Book[0].Author, Is.EqualTo(targetBook.Author));
            Assert.That(result.Book[0].Year, Is.EqualTo(targetBook.Year));
            Assert.That(result.Book[0].Publisher, Is.EqualTo(targetBook.Publisher));
        });
    }

    [Test]
    public async Task Execute_GetNonExistingBook_ReturnsNull()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = (FakeRepository<Book>)unitOfWork.GetRepository<Book>();
        await bookRepo.AddRangeAsync(new Bogus.Faker<Book>()
                                   .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
                                   .RuleFor(x => x.Title, f => f.Lorem.Sentence(3))
                                   .RuleFor(x => x.Author, f => f.Name.FullName())
                                   .RuleFor(x => x.Year, f => f.Random.Int(1900, 2024))
                                   .RuleFor(x => x.Publisher, f => f.Company.CompanyName())
                                   .RuleFor(x => x.RoomBook, f => [])
                                   .Generate(10)
                                   .AsEnumerable());

        var nonExistingId = new Id(Guid.NewGuid());
        var getBookQuery = new GetBookCommand(unitOfWork, Converter);
        var getBookRequest = new GetBookRequest { Id = nonExistingId };

        // Act & Assert
        Assert.ThrowsAsync<BookNotFoundException>(async () =>
            await getBookQuery.Execute(getBookRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_GetBookFromEmptyRepository_ReturnsNull()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = (FakeRepository<Book>)unitOfWork.GetRepository<Book>();
        var getBookQuery = new GetBookCommand(unitOfWork, Converter);
        var getBookRequest = new GetBookRequest { Id = new Id(Guid.NewGuid()) };

        // Act & Assert
        Assert.ThrowsAsync<BookNotFoundException>(async () =>
            await getBookQuery.Execute(getBookRequest, CancellationToken.None));
    }
}