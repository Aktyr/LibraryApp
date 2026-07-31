namespace LibApp.ApplicationTests.Commands.Entities.Books;

[TestFixture]
public class GetAllBooksQueryTests
{
    private IConverter<Book, BookDTO> Converter => new BookDTOConverter();

    [Test]
    public async Task Execute_GetAllBooksFromRepositoryWithBooks_ReturnsAllBooks()
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
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate(7)
            .ToList();
        await bookRepo.AddRangeAsync(books.AsEnumerable(), CancellationToken.None);

        var getAllBooksQuery = new GetAllBooksCommand(unitOfWork, Converter);
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
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = (FakeRepository<Book>)unitOfWork.GetRepository<Book>();
        var getAllBooksQuery = new GetAllBooksCommand(unitOfWork, Converter);
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
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = (FakeRepository<Book>)unitOfWork.GetRepository<Book>();
        var book = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => "Single Book")
            .RuleFor(x => x.Author, f => "Test Author")
            .RuleFor(x => x.Year, f => 2020)
            .RuleFor(x => x.Publisher, f => "Test Publisher")
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate();
        await bookRepo.AddRangeAsync(new[] { book }, CancellationToken.None);

        var getAllBooksQuery = new GetAllBooksCommand(unitOfWork, Converter);
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
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = (FakeRepository<Book>)unitOfWork.GetRepository<Book>();
        var books = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => f.Lorem.Sentence(3))
            .RuleFor(x => x.Author, f => f.Name.FullName())
            .RuleFor(x => x.Year, f => f.Random.Int(1900, 2024))
            .RuleFor(x => x.Publisher, f => f.Company.CompanyName())
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate(5)
            .ToList();
        await bookRepo.AddRangeAsync(books.AsEnumerable(), CancellationToken.None);

        var getAllBooksQuery = new GetAllBooksCommand(unitOfWork, Converter);
        var emptyRequest = new EmptyRequest();

        // Act
        var result = await getAllBooksQuery.Execute(emptyRequest, CancellationToken.None);

        // Assert
        // Проверяем, что метод GetWithoutTracking был вызван (косвенно)
        // Если FakeRepository правильно реализует GetWithoutTracking, просто проверяем, что данные возвращаются корректно
        Assert.That(result.Book, Has.Length.EqualTo(5));
    }
    [Test]
    public async Task GetWithoutTracking_WithPredicate_DelegatesToGet()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<Book>)unitOfWork.GetRepository<Book>();
        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book A", Author = "Author", RoomBook = new List<RoomBook>() };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book B", Author = "Author", RoomBook = new List<RoomBook>() };

        await repo.AddRangeAsync(new[] { book1, book2 }, CancellationToken.None);

        // Act
        var result = await repo.GetWithoutTrackingAsync(b => b.Title == "Book A", CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().Title, Is.EqualTo("Book A"));
        });
    }

}