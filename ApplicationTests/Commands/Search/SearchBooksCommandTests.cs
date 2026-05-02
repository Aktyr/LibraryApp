namespace LibApp.ApplicationTests.Commands.Search;

[TestFixture]
public class SearchBooksCommandTests
{
    private IConverter<Book, BookDTO> Converter => new BookDTOConverter();

    [Test]
    public async Task Execute_WhenNoBooks_ReturnsEmptyResult()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();
        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest();

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Message, Does.Contain("Найдено книг: 0"));
            Assert.That(result.Book, Is.Empty);
        });
    }

    [Test]
    public async Task Execute_WithBooks_ReturnsAllBooks()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var books = new Bogus.Faker<Book>()
            .RuleFor(x => x.Id, f => new Id(Guid.NewGuid()))
            .RuleFor(x => x.Title, f => f.Lorem.Sentence(3))
            .RuleFor(x => x.Author, f => f.Name.FullName())
            .RuleFor(x => x.Year, f => f.Random.Int(1900, 2024))
            .RuleFor(x => x.Publisher, f => f.Company.CompanyName())
            .RuleFor(x => x.RoomBook, f => new List<RoomBook>())
            .Generate(5)
            .ToList();
        await bookRepo.AddRange(books);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest();

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Message, Does.Contain("Найдено книг: 5"));
            Assert.That(result.Book, Has.Length.EqualTo(5));
        });
    }

    [Test]
    public async Task Execute_WithQuery_FiltersByTitleAuthorOrPublisher()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Matching Title", Author = "Author A", Publisher = "Publisher X", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Another Book", Author = "Matching Author", Publisher = "Publisher Y", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Something Else", Author = "Author C", Publisher = "Matching Publisher", RoomBook = [] };
        var book4 = new Book { Id = new Id(Guid.NewGuid()), Title = "No Match", Author = "No Match", Publisher = "Nothing", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3, book4]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { Query = "matching" };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Book, Has.Length.EqualTo(3));
            Assert.That(result.Book.Select(b => b.Id), Is.EquivalentTo(new[] { book1.Id.Value, book2.Id.Value, book3.Id.Value }));
        });
    }

    [Test]
    public async Task Execute_WithTitleFilter_ReturnsMatchingBooks()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Clean Code", Author = "Martin", Publisher = "Prentice", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Design Patterns", Author = "Gamma", Publisher = "Addison", RoomBook = [] };

        await bookRepo.AddRange([book1, book2]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { Title = "Clean" };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Book, Has.Length.EqualTo(1));
            Assert.That(result.Book[0].Title, Is.EqualTo("Clean Code"));
        });
    }

    [Test]
    public async Task Execute_WithAuthorFilter_ReturnsMatchingBooks()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1", Author = "Robert C. Martin", Publisher = "Pub A", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2", Author = "Erich Gamma", Publisher = "Pub B", RoomBook = [] };

        await bookRepo.AddRange([book1, book2]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { Author = "martin" };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Book, Has.Length.EqualTo(1));
            Assert.That(result.Book[0].Author, Is.EqualTo("Robert C. Martin"));
        });
    }

    [Test]
    public async Task Execute_WithYearFromFilter_ReturnsBooksAfterYear()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Old Book", Author = "Author", Year = 1990, Publisher = "Pub", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "New Book", Author = "Author", Year = 2010, Publisher = "Pub", RoomBook = [] };

        await bookRepo.AddRange([book1, book2]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { YearFrom = 2000 };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Book, Has.Length.EqualTo(1));
            Assert.That(result.Book[0].Title, Is.EqualTo("New Book"));
        });
    }

    [Test]
    public async Task Execute_WithYearToFilter_ReturnsBooksBeforeYear()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Old Book", Author = "Author", Year = 1990, Publisher = "Pub", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "New Book", Author = "Author", Year = 2010, Publisher = "Pub", RoomBook = [] };

        await bookRepo.AddRange([book1, book2]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { YearTo = 2000 };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Book, Has.Length.EqualTo(1));
            Assert.That(result.Book[0].Title, Is.EqualTo("Old Book"));
        });
    }

    [Test]
    public async Task Execute_WithPublisherFilter_ReturnsMatchingBooks()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1", Author = "Author", Publisher = "Prentice Hall", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2", Author = "Author", Publisher = "Addison Wesley", RoomBook = [] };

        await bookRepo.AddRange([book1, book2]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { Publisher = "prentice" };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Book, Has.Length.EqualTo(1));
            Assert.That(result.Book[0].Publisher, Is.EqualTo("Prentice Hall"));
        });
    }

    [Test]
    public async Task Execute_WithAvailableOnlyFilter_ReturnsOnlyAvailableBooks()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var availableBookId = new Id(Guid.NewGuid());
        var unavailableBookId = new Id(Guid.NewGuid());

        var availableBook = new Book { Id = availableBookId, Title = "Available", Author = "Author", RoomBook = [] };
        var unavailableBook = new Book { Id = unavailableBookId, Title = "Unavailable", Author = "Author", RoomBook = [] };

        await bookRepo.AddRange([availableBook, unavailableBook]);

        // Книга с доступными экземплярами: BookCount > BorrowedCount
        var availableRoomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 2, // AvailableCount = 3 > 0
            Book = availableBook,
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        // Книга без доступных экземпляров: BookCount == BorrowedCount
        var unavailableRoomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BookCount = 5,
            BorrowedCount = 5, // AvailableCount = 0
            Book = unavailableBook,
            Room = new Room { Id = new Id(Guid.NewGuid()) }
        };

        await roomBookRepo.AddRange([availableRoomBook, unavailableRoomBook]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { AvailableOnly = true };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Book, Has.Length.EqualTo(1));
            Assert.That(result.Book[0].Title, Is.EqualTo("Available"));
        });
    }

    [Test]
    public async Task Execute_WithSortByTitleDescending_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "C Book", Author = "Author", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "A Book", Author = "Author", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "B Book", Author = "Author", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "title", SortDescending = true };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "C Book", "B Book", "A Book" }));
    }

    [Test]
    public async Task Execute_WithSortByAuthor_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1", Author = "Charlie", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2", Author = "Alice", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 3", Author = "Bob", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "author", SortDescending = false };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "Book 2", "Book 3", "Book 1" }));
    }
    [Test]
    public async Task Execute_WithSortByAuthorAscending_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1", Author = "Charlie", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2", Author = "Alice", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 3", Author = "Bob", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "author", SortDescending = false };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "Book 2", "Book 3", "Book 1" }));
    }

    [Test]
    public async Task Execute_WithSortByAuthorDescending_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1", Author = "Charlie", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2", Author = "Alice", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 3", Author = "Bob", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "author", SortDescending = true };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "Book 1", "Book 3", "Book 2" }));
    }

    [Test]
    public async Task Execute_WithSortByYearAscending_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Old Book", Author = "Author", Year = 1990, RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "New Book", Author = "Author", Year = 2020, RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Mid Book", Author = "Author", Year = 2005, RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "year", SortDescending = false };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "Old Book", "Mid Book", "New Book" }));
    }

    [Test]
    public async Task Execute_WithSortByYearDescending_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Old Book", Author = "Author", Year = 1990, RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "New Book", Author = "Author", Year = 2020, RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Mid Book", Author = "Author", Year = 2005, RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "year", SortDescending = true };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "New Book", "Mid Book", "Old Book" }));
    }

    [Test]
    public async Task Execute_WithSortByPublisherAscending_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1", Author = "Author", Publisher = "Zed Books", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2", Author = "Author", Publisher = "Alpha Press", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 3", Author = "Author", Publisher = "Middle Pub", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "publisher", SortDescending = false };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "Book 2", "Book 3", "Book 1" }));
    }

    [Test]
    public async Task Execute_WithSortByPublisherDescending_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1", Author = "Author", Publisher = "Zed Books", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2", Author = "Author", Publisher = "Alpha Press", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 3", Author = "Author", Publisher = "Middle Pub", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "publisher", SortDescending = true };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "Book 1", "Book 3", "Book 2" }));
    }

    [Test]
    public async Task Execute_WithSortByPopularityAscending_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Popular Book",
            Author = "Author",
            RoomBook = new List<RoomBook>
        {
            new RoomBook { Id = new Id(Guid.NewGuid()), BookCount = 5, BorrowedCount = 10 }
        }
        };
        var book2 = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Unpopular Book",
            Author = "Author",
            RoomBook = new List<RoomBook>
        {
            new RoomBook { Id = new Id(Guid.NewGuid()), BookCount = 5, BorrowedCount = 1 }
        }
        };
        var book3 = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Mid Book",
            Author = "Author",
            RoomBook = new List<RoomBook>
        {
            new RoomBook { Id = new Id(Guid.NewGuid()), BookCount = 5, BorrowedCount = 5 },
            new RoomBook { Id = new Id(Guid.NewGuid()), BookCount = 3, BorrowedCount = 2 }
        }
        };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "popularity", SortDescending = false };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        // Сортировка по сумме BorrowedCount: book2=1, book3=7, book1=10
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "Unpopular Book", "Mid Book", "Popular Book" }));
    }

    [Test]
    public async Task Execute_WithSortByPopularityDescending_ReturnsBooksSorted()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Popular Book",
            Author = "Author",
            RoomBook = new List<RoomBook>
        {
            new RoomBook { Id = new Id(Guid.NewGuid()), BookCount = 5, BorrowedCount = 10 }
        }
        };
        var book2 = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Unpopular Book",
            Author = "Author",
            RoomBook = new List<RoomBook>
        {
            new RoomBook { Id = new Id(Guid.NewGuid()), BookCount = 5, BorrowedCount = 1 }
        }
        };
        var book3 = new Book
        {
            Id = new Id(Guid.NewGuid()),
            Title = "Mid Book",
            Author = "Author",
            RoomBook = new List<RoomBook>
        {
            new RoomBook { Id = new Id(Guid.NewGuid()), BookCount = 5, BorrowedCount = 5 },
            new RoomBook { Id = new Id(Guid.NewGuid()), BookCount = 3, BorrowedCount = 2 }
        }
        };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "popularity", SortDescending = true };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "Popular Book", "Mid Book", "Unpopular Book" }));
    }

    [Test]
    public async Task Execute_WithInvalidSortBy_DefaultsToSortByTitle()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "C Book", Author = "Author", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "A Book", Author = "Author", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "B Book", Author = "Author", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = "invalid_field", SortDescending = false };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "A Book", "B Book", "C Book" }));
    }
    [Test]
    public async Task Execute_WithCombinedFilters_ReturnsMatchingBooks()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Matching Title", Author = "Correct Author", Year = 2005, Publisher = "Test Pub", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Wrong Title", Author = "Correct Author", Year = 2005, Publisher = "Test Pub", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "Matching Title", Author = "Wrong Author", Year = 1990, Publisher = "Test Pub", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest
        {
            Title = "Matching",
            YearFrom = 2000
        };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Book, Has.Length.EqualTo(1));
            Assert.That(result.Book[0].Id, Is.EqualTo(book1.Id.Value));
        });
    }

    [Test]
    public async Task Execute_WithYearRangeFilter_ReturnsBooksInRange()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Old Book", Author = "Author", Year = 1990, Publisher = "Pub", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Mid Book", Author = "Author", Year = 2000, Publisher = "Pub", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "New Book", Author = "Author", Year = 2010, Publisher = "Pub", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { YearFrom = 1995, YearTo = 2005 };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Book, Has.Length.EqualTo(1));
            Assert.That(result.Book[0].Title, Is.EqualTo("Mid Book"));
        });
    }

    [Test]
    public async Task Execute_WithDefaultSortBy_ReturnsBooksSortedByTitle()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "C Book", Author = "Author", RoomBook = [] };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "A Book", Author = "Author", RoomBook = [] };
        var book3 = new Book { Id = new Id(Guid.NewGuid()), Title = "B Book", Author = "Author", RoomBook = [] };

        await bookRepo.AddRange([book1, book2, book3]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { SortBy = null }; // Default to title

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.That(result.Book.Select(b => b.Title), Is.EqualTo(new[] { "A Book", "B Book", "C Book" }));
    }

    [Test]
    public async Task Execute_WithZeroResults_ReturnsEmptyArray()
    {
        // Arrange
        var bookRepo = new FakeRepository<Book>();
        var roomBookRepo = new FakeRepository<RoomBook>();

        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Some Book", Author = "Author", Year = 2020, Publisher = "Pub", RoomBook = [] };

        await bookRepo.AddRange([book1]);

        var command = new SearchBooksCommand(bookRepo, roomBookRepo, Converter);
        var request = new SearchBooksRequest { Title = "NonExistent" };

        // Act
        var result = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Message, Does.Contain("Найдено книг: 0"));
            Assert.That(result.Book, Is.Empty);
        });
    }

}