using LibApp.Application.Configuration;

namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class GetUserRoomBooksQueryTests
{
    private BorrowingValidatorAsync CreateValidator()
    {
        var optionsMock = new Mock<IOptionsSnapshot<BorrowingSettings>>();
        optionsMock.Setup(x => x.Value).Returns(new BorrowingSettings());
        return new BorrowingValidatorAsync(optionsMock.Object);
    }

    [Test]
    public async Task Execute_WhenUserHasNoBooks_ReturnsEmptyList()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var converter = new BorrowDTOConverter(); // Используем реальный конвертер для поднятия его coverage
        var query = new GetUserRoomBooksQuery(unitOfWork, converter, CreateValidator());

        var request = new GetUserBooksRequest { UserId = Guid.NewGuid() };

        // Act
        var result = await query.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo("Ok"));
            Assert.That(result.Message, Is.EqualTo("Список книг получен"));
            Assert.That(result.Books, Is.Empty);
        });
    }

    [Test]
    public async Task Execute_WhenUserHasBooks_ReturnsSortedDTOs()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var repo = (FakeRepository<UserRoomBook>)unitOfWork.GetRepository<UserRoomBook>();
        var converter = new BorrowDTOConverter();

        var userId = Guid.NewGuid();
        var user = new User { Id = new Id(userId) };

        var urb1 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = DateTime.Now.AddDays(10), // Сдавать позже
            BorrowDate = DateTime.Now.AddDays(-2),
            RoomBook = new RoomBook { Book = new Book { Title = "Book 1", Author = "Author 1" }, Room = new Room { Name = "Room A" } }
        };

        var urb2 = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            Deadline = DateTime.Now.AddDays(2), // Сдавать раньше
            BorrowDate = DateTime.Now.AddDays(-5),
            RoomBook = new RoomBook { Book = new Book { Title = "Book 2", Author = "Author 2" }, Room = new Room { Name = "Room B" } }
        };

        await repo.AddRangeAsync(new[] { urb1, urb2 }, CancellationToken.None);

        var query = new GetUserRoomBooksQuery(unitOfWork, converter, CreateValidator());
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
        });
    }
}