namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class GetUserRoomBooksQueryTests
{
    private static BorrowDTOConverter CreateConverter()
    {
        var optionsMock = new Mock<IOptionsSnapshot<PenaltySettings>>();
        optionsMock.Setup(x => x.Value).Returns(new PenaltySettings());
        var penaltyService = new PenaltyCalculatorService(optionsMock.Object);
        return new BorrowDTOConverter(penaltyService);
    }

    [Test]
    public async Task Execute_WhenUserHasNoBooks_ReturnsEmptyList()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var converter = CreateConverter();
        var query = new GetUserRoomBooksCommand(unitOfWork, converter);

        var request = new GetUserBooksRequest { UserId = Guid.NewGuid() };

        // Act
        var result = await query.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Books, Is.Empty);
        });
    }

    [Test]
    public async Task Execute_WhenUserHasBooks_ReturnsSortedDTOs()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();

        var converter = CreateConverter();
        var userId = Guid.NewGuid();
        var user = new User { Id = new Id(userId) };
        // FK обязательны: BorrowDTOConverter читает RoomBook.BookId.Value,
        // без FK поле Id остаётся null и .Value бросает NullReferenceException.
        var book1 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 1", Author = "Author 1" };
        var book2 = new Book { Id = new Id(Guid.NewGuid()), Title = "Book 2", Author = "Author 2" };
        var urb1 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = DateTime.Now.AddDays(10), // Сдавать позже
            BorrowDate = DateTime.Now.AddDays(-2),
            RoomBook = new RoomBook { BookId = book1.Id, Book = book1, Room = new Room { Name = "Room A" } }
        };

        var urb2 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = DateTime.Now.AddDays(2), // Сдавать раньше
            BorrowDate = DateTime.Now.AddDays(-5),
            RoomBook = new RoomBook { BookId = book2.Id, Book = book2, Room = new Room { Name = "Room B" } }
        };

        await repo.AddRangeAsync([urb1, urb2], CancellationToken.None);

        var query = new GetUserRoomBooksCommand(unitOfWork, converter);
        var request = new GetUserBooksRequest { UserId = userId };

        // Act
        var result = await query.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Books, Has.Length.EqualTo(2));

            // Проверка сортировки: OrderBy(b => b.Deadline) означает, что urb2 должен идти первым
            Assert.That(result.Books[0].BookTitle, Is.EqualTo("Book 2"));
            Assert.That(result.Books[1].BookTitle, Is.EqualTo("Book 1"));

            // Проверка работы конвертера
            Assert.That(result.Books[0].RoomName, Is.EqualTo("Room B"));
            Assert.That(result.Books[0].Borrow, Is.EqualTo(urb2.BorrowDate));
            Assert.That(result.Books[0].BookId, Is.EqualTo(book2.Id.Value));
        });
    }
}