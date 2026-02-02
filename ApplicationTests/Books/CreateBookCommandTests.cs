namespace LibApp.ApplicationTests.Books;

[TestFixture]
public class CreateBookCommandTests
{
    [TestCase("Clean Code", "Robert C. Martin", 2008, "Prentice Hall")]
    [TestCase("Design Patterns", "Erich Gamma", 1994, "Addison-Wesley")]
    [TestCase("Refactoring", "Martin Fowler", 1999, "Addison-Wesley")]
    [TestCase("The Clean Coder", "Robert C. Martin", 2011, "Prentice Hall")]
    public async Task Execute_CreateBookWithValidData_CreatesBook(string title, string author, int year, string publisher)
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
        var createBookCommand = new CreateBookCommand(bookRepo);
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
            Assert.That((await bookRepo.Get(x => x.Title == title)).Any(), Is.True);
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(bookRepo.Entities, Has.Count.EqualTo(11));
        });
    }

    [TestCase("", "Author", 2000, "Publisher")]
    [TestCase("Title", "", 2000, "Publisher")]
    [TestCase("Title", "Author", -1, "Publisher")] // Не валидно
    [TestCase("Title", "Author", 3020, "Publisher")] // Не валидно
    [TestCase("Title", "Author", 2000, "")]
    public async Task Execute_CreateBookWithInvalidData_ReturnsError(string title, string author, int year, string publisher)
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
        var createBookCommand = new CreateBookCommand(bookRepo);
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
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Error"));
            Assert.That(response.Message, Contains.Substring("Ошибки валидации"));
        });
    }
}