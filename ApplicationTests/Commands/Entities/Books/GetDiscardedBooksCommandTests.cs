namespace LibApp.ApplicationTests.Commands.Entities.Books;

[TestFixture]
public class GetDiscardedBooksCommandTests
{
    [Test]
    public async Task Execute_WithoutFilters_ReturnsAllDiscardedBooks()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var now = DateTime.UtcNow;
        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2" };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Room" };

        var discarded1 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book1,
            Room = room,
            RoomId = room.Id,
            Amount = 2,
            DiscardedDate = now.AddDays(-5),
            DiscardReason = DiscardReason.Wear,
            ApprovedBy = "Admin"
        };
        var discarded2 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book2,
            Room = room,
            RoomId = room.Id,
            Amount = 1,
            DiscardedDate = now.AddDays(-2),
            DiscardReason = DiscardReason.Loss,
            ApprovedBy = "Librarian"
        };

        await discardedRepo.AddRangeAsync([discarded1, discarded2], CancellationToken.None);

        var command = new GetDiscardedBooksCommand(unitOfWork);
        var request = new GetDiscardedBooksRequest();

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Discarded, Has.Length.EqualTo(2));
            Assert.That(response.Discarded.Select(d => d.Id),
                Is.EquivalentTo(new[] { discarded1.Id.Value, discarded2.Id.Value }));
        });
    }

    [Test]
    public async Task Execute_WithFromDateFilter_ReturnsOnlyBooksDiscardedAfterDate()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var now = DateTime.UtcNow;
        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2" };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Room" };

        var discarded1 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book1,
            Room = room,
            RoomId = room.Id,
            Amount = 2,
            DiscardedDate = now.AddDays(-10),
            DiscardReason = DiscardReason.Wear
        };
        var discarded2 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book2,
            Room = room,
            RoomId = room.Id,
            Amount = 1,
            DiscardedDate = now.AddDays(-3),
            DiscardReason = DiscardReason.Loss
        };

        await discardedRepo.AddRangeAsync([discarded1, discarded2], CancellationToken.None);

        var command = new GetDiscardedBooksCommand(unitOfWork);
        var request = new GetDiscardedBooksRequest
        {
            FromDate = now.AddDays(-5)
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Discarded, Has.Length.EqualTo(1));
            Assert.That(response.Discarded[0].Id, Is.EqualTo(discarded2.Id.Value));
        });
    }

    [Test]
    public async Task Execute_WithToDateFilter_ReturnsOnlyBooksDiscardedBeforeDate()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var now = DateTime.UtcNow;
        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2" };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Room" };

        var discarded1 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book1,
            Room = room,
            RoomId = room.Id,
            Amount = 2,
            DiscardedDate = now.AddDays(-10),
            DiscardReason = DiscardReason.Wear
        };
        var discarded2 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book2,
            Room = room,
            RoomId = room.Id,
            Amount = 1,
            DiscardedDate = now.AddDays(-3),
            DiscardReason = DiscardReason.Loss
        };

        await discardedRepo.AddRangeAsync([discarded1, discarded2], CancellationToken.None);

        var command = new GetDiscardedBooksCommand(unitOfWork);
        var request = new GetDiscardedBooksRequest
        {
            ToDate = now.AddDays(-5)
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Discarded, Has.Length.EqualTo(1));
            Assert.That(response.Discarded[0].Id, Is.EqualTo(discarded1.Id.Value));
        });
    }

    [Test]
    public async Task Execute_WithDiscardReasonFilter_ReturnsOnlyBooksWithThatReason()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var now = DateTime.UtcNow;
        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2" };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Room" };

        var discarded1 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book1,
            Room = room,
            RoomId = room.Id,
            Amount = 2,
            DiscardedDate = now,
            DiscardReason = DiscardReason.Wear
        };
        var discarded2 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book2,
            Room = room,
            RoomId = room.Id,
            Amount = 1,
            DiscardedDate = now,
            DiscardReason = DiscardReason.Loss
        };

        await discardedRepo.AddRangeAsync([discarded1, discarded2], CancellationToken.None);

        var command = new GetDiscardedBooksCommand(unitOfWork);
        var request = new GetDiscardedBooksRequest
        {
            DiscardReason = DiscardReason.Wear
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Discarded, Has.Length.EqualTo(1));
            Assert.That(response.Discarded[0].Id, Is.EqualTo(discarded1.Id.Value));
        });
    }

    [Test]
    public async Task Execute_WithCombinedFilters_ReturnsCorrectBooks()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var now = DateTime.UtcNow;
        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1" };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2" };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 3" };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Room" };

        var discarded1 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book1,
            Room = room,
            RoomId = room.Id,
            Amount = 2,
            DiscardedDate = now.AddDays(-10),
            DiscardReason = DiscardReason.Wear
        };
        var discarded2 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book2,
            Room = room,
            RoomId = room.Id,
            Amount = 1,
            DiscardedDate = now.AddDays(-3),
            DiscardReason = DiscardReason.Wear
        };
        var discarded3 = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book3,
            Room = room,
            RoomId = room.Id,
            Amount = 3,
            DiscardedDate = now.AddDays(-1),
            DiscardReason = DiscardReason.Loss
        };

        IEnumerable<DiscardedBook> entities = [discarded1, discarded2, discarded3];
        await discardedRepo.AddRangeAsync(entities, CancellationToken.None);

        var command = new GetDiscardedBooksCommand(unitOfWork);
        var request = new GetDiscardedBooksRequest
        {
            FromDate = now.AddDays(-5),
            ToDate = now.AddDays(0),
            DiscardReason = DiscardReason.Wear
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Discarded, Has.Length.EqualTo(1));
            Assert.That(response.Discarded[0].Id, Is.EqualTo(discarded2.Id.Value));
        });
    }

    [Test]
    public async Task Execute_WhenNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();

        var now = DateTime.UtcNow;
        var book = new Book { Id = new Id(Guid.NewGuid()), Title = "Book" };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Room" };

        var discarded = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = room,
            RoomId = room.Id,
            Amount = 1,
            DiscardedDate = now,
            DiscardReason = DiscardReason.Wear
        };

        await discardedRepo.AddRangeAsync([discarded], CancellationToken.None);

        var command = new GetDiscardedBooksCommand(unitOfWork);
        var request = new GetDiscardedBooksRequest
        {
            FromDate = now.AddDays(1),
            ToDate = now.AddDays(2),
            DiscardReason = DiscardReason.Loss
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Discarded, Is.Empty);
        });
    }
}