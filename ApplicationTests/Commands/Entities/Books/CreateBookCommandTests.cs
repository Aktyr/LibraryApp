namespace LibApp.ApplicationTests.Commands.Entities.Books;

[TestFixture]   
public class CreateBookCommandTests
{
    private BookValidatorAsync CreateBookValidator => new();
    private IConverter<Book, BookDTO> Converter => new BookDTOConverter();

    [TestCase("Clean Code", "Robert C. Martin", 2008, "Prentice Hall")]
    [TestCase("Design Patterns", "Erich Gamma", 1994, "Addison-Wesley")]
    [TestCase("Refactoring", "Martin Fowler", 1999, "Addison-Wesley")]
    [TestCase("The Clean Coder", "Robert C. Martin", 2011, "Prentice Hall")]
    [TestCase("A", "A", 0, "A")]
    [TestCase("1", "1", 0, "1")]
    public async Task Execute_CreateBookWithValidData_CreatesBook(string title, string author, int year, string publisher)
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
                                   .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
                                   .Generate(10)
                                   .AsEnumerable());
        var createBookCommand = new CreateBookCommand(unitOfWork, CreateBookValidator, Converter);
        var createBookRequest = new CreateBookRequest
        {
            Title = title,
            Author = author,
            Year = year,
            Publisher = publisher
        };

        // Act
        var response = await createBookCommand.Execute(createBookRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That((await bookRepo.GetAsync(x => x.Title == title)).Any(), Is.True);
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(bookRepo.Entities, Has.Count.EqualTo(11));
        });
    }

    [TestCase("", "Author", 2000, "Publisher")]
    [TestCase("Title", "", 2000, "Publisher")]
    [TestCase("Title", "Author", -1, "Publisher")] 
    [TestCase("Title", "Author", 3000, "Publisher")] 
    [TestCase("", "", 3000, "")]
    [TestCase("Title", "Author", 3000, "")]
    [TestCase("", "Author", 2000, "")]
    [TestCase("Title", "", 3000, "Publisher")]
    [TestCase("", "", 3000, "Publisher")]
    [TestCase("Title", "", 3000, "")]
    public async Task Execute_CreateBookWithInvalidData_ReturnsError(string title, string author, int year, string publisher)
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
                                   .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
                                   .Generate(10)
                                   .AsEnumerable());
        var createBookCommand = new CreateBookCommand(unitOfWork, CreateBookValidator, Converter);
        var createBookRequest = new CreateBookRequest
        {
            Title = title,
            Author = author,
            Year = year,
            Publisher = publisher
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await createBookCommand.Execute(createBookRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_CreateBookWithTooLongTitle_ThrowsValidationException()
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

        var createBookCommand = new CreateBookCommand(unitOfWork, CreateBookValidator, Converter);
        var createBookRequest = new CreateBookRequest
        {
            Title = new string('A', 101),
            Author = "Valid Author",
            Year = 2000,
            Publisher = "Valid Publisher"
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await createBookCommand.Execute(createBookRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_CreateBookWithTooLongAuthor_ThrowsValidationException()
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
                                   .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
                                   .Generate(10)
                                   .AsEnumerable());

        var createBookCommand = new CreateBookCommand(unitOfWork, CreateBookValidator, Converter);
        var createBookRequest = new CreateBookRequest
        {
            Title = "Valid Title",
            Author = new string('B', 101),
            Year = 2000,
            Publisher = "Valid Publisher"
        };

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await createBookCommand.Execute(createBookRequest, CancellationToken.None));
    }
}