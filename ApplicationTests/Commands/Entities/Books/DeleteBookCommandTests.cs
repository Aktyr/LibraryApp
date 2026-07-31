namespace LibApp.ApplicationTests.Commands.Entities.Books;

[TestFixture]
public class DeleteBookCommandTests
{
    [Test]
    public async Task Execute_DeleteExistingBook_DeletesBook()
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
            .Generate(10)
            .ToList();
        await bookRepo.AddRangeAsync(books.AsEnumerable(), CancellationToken.None);

        var bookToDelete = books[5];
        var deleteBookCommand = new DeleteBookCommand(unitOfWork);
        var deleteBookRequest = new DeleteBookRequest { Id = bookToDelete.Id };

        // Act
        var response = await deleteBookCommand.Execute(deleteBookRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Is.EqualTo("Book deleted successfully."));
            Assert.That(bookRepo.Entities, Has.Count.EqualTo(9));
            Assert.That((await bookRepo.GetAsync(x => x.Id.Value == bookToDelete.Id.Value)).Any(), Is.False);
        });
    }

    [Test]
    public async Task Execute_DeleteNonExistingBook_ThrowsBookNotFoundException()
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
                                   .AsEnumerable(), CancellationToken.None);

        var nonExistingId = new Id(Guid.NewGuid());
        var deleteBookCommand = new DeleteBookCommand(unitOfWork);
        var deleteBookRequest = new DeleteBookRequest { Id = nonExistingId };

        // Act & Assert
        Assert.ThrowsAsync<BookNotFoundException>(async () =>
            await deleteBookCommand.Execute(deleteBookRequest, CancellationToken.None));
    }


    [Test]
    public async Task Execute_DeleteBookFromEmptyRepository_ThrowsBookNotFoundException()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = (FakeRepository<Book>)unitOfWork.GetRepository<Book>();
        var deleteBookCommand = new DeleteBookCommand(unitOfWork);
        var deleteBookRequest = new DeleteBookRequest { Id = new Id(Guid.NewGuid()) };

        // Act & Assert
        Assert.ThrowsAsync<BookNotFoundException>(async () =>
            await deleteBookCommand.Execute(deleteBookRequest, CancellationToken.None));
    }

    [Test]
    public async Task Execute_DeleteLastBook_RepositoryBecomesEmpty()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var bookRepo = (FakeRepository<Book>)unitOfWork.GetRepository<Book>();
        var book = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => "Last Book")
            .RuleFor(x => x.Author, f => f.Name.FullName())
            .RuleFor(x => x.Year, f => f.Random.Int(1900, 2024))
            .RuleFor(x => x.Publisher, f => f.Company.CompanyName())
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate();
        await bookRepo.AddRangeAsync(new[] { book }, CancellationToken.None);

        var deleteBookCommand = new DeleteBookCommand(unitOfWork);
        var deleteBookRequest = new DeleteBookRequest { Id = book.Id };

        // Act
        var response = await deleteBookCommand.Execute(deleteBookRequest, CancellationToken.None);

        // Assert
        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(bookRepo.Entities, Has.Count.EqualTo(0));
            Assert.That((await bookRepo.GetAsync()).Any(), Is.False);
        });
    }
}