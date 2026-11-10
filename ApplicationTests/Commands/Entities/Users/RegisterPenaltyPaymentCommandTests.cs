namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class RegisterPenaltyPaymentCommandTests
{
    /// <summary>Собирает граф User → UserRoomBook → RoomBook/Book для команды (аудит читает RoomBook.Book.Title и User.FullName).</summary>
    private static (FakeUnitOfWork uow, UserRoomBook urb, Book book) CreateScenario(
        decimal? penalty = 100m,
        bool isReturned = true,
        bool alreadyPaid = false)
    {
        var uow = new FakeUnitOfWork();
        var urbRepo = (FakeRepository<UserRoomBook>)uow.GetRepository<UserRoomBook>();
        var paymentRepo = (FakeRepository<PenaltyPayment>)uow.GetRepository<PenaltyPayment>();

        var book = new Book { Id = new Id(Guid.NewGuid()), Title = "Test Book", Author = "Author" };
        var room = new Room { Id = new Id(Guid.NewGuid()), Name = "Test Room" };
        var reader = new User
        {
            Id = new Id(Guid.NewGuid()),
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            ContactInfo = "reader@test.com",
            Email = "reader@test.com",
            RoomBooks = []
        };

        var roomBook = new RoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = room,
            BookCount = 1,
            BorrowedCount = 0
        };

        var urb = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = reader,
            RoomBook = roomBook,
            BorrowDate = DateTime.UtcNow.AddDays(-10),
            Deadline = DateTime.UtcNow.AddDays(-3),
            ReturnDate = isReturned ? DateTime.UtcNow.AddDays(-1) : null,
            Penalty = penalty
        };

        if (alreadyPaid)
        {
            urb.PenaltyPayments.Add(new PenaltyPayment
            {
                Id = new Id(Guid.NewGuid()),
                UserRoomBookId = urb.Id,
                UserRoomBook = urb,
                Amount = penalty ?? 0m,
                PaidAt = DateTime.UtcNow.AddDays(-1),
                PaidByUserId = new Id(Guid.NewGuid()),
                PaidByUser = new User { Id = new Id(Guid.NewGuid()), RoomBooks = [] }
            });
        }

        urbRepo.AddRangeAsync([urb], CancellationToken.None).GetAwaiter().GetResult();

        return (uow, urb, book);
    }

    [Test]
    public async Task Execute_WhenUserRoomBookNotFound_ThrowsUserRoomBookNotFoundException()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var command = new RegisterPenaltyPaymentCommand(
            uow, new FakeCurrentUserService(), Mock.Of<IAuditService>());

        var request = new RegisterPenaltyPaymentRequest { UserRoomBookId = Guid.NewGuid() };

        // Act & Assert
        await Assert.ThrowsAsync<UserRoomBookNotFoundException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenNoPenalty_ThrowsValidationException()
    {
        // Arrange
        var (uow, urb, _) = CreateScenario(penalty: null);
        var command = new RegisterPenaltyPaymentCommand(
            uow, new FakeCurrentUserService(), Mock.Of<IAuditService>());

        var request = new RegisterPenaltyPaymentRequest { UserRoomBookId = urb.Id.Value };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Has.Member("Нет штрафа для оплаты"));
    }

    [Test]
    public async Task Execute_WhenBookNotReturned_ThrowsValidationException()
    {
        // Arrange
        var (uow, urb, _) = CreateScenario(penalty: 100m, isReturned: false);
        var command = new RegisterPenaltyPaymentCommand(
            uow, new FakeCurrentUserService(), Mock.Of<IAuditService>());

        var request = new RegisterPenaltyPaymentRequest { UserRoomBookId = urb.Id.Value };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Has.Member("Штраф можно оплатить только после возврата книги"));
    }

    [Test]
    public async Task Execute_WhenAlreadyPaid_ThrowsValidationException()
    {
        // Arrange
        var (uow, urb, _) = CreateScenario(penalty: 100m, alreadyPaid: true);
        var command = new RegisterPenaltyPaymentCommand(
            uow, new FakeCurrentUserService(), Mock.Of<IAuditService>());

        var request = new RegisterPenaltyPaymentRequest { UserRoomBookId = urb.Id.Value };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.ExceptionDetails, Has.Member("Штраф уже оплачен"));
    }

    [Test]
    public async Task Execute_WithValidData_CreatesPaymentAndWritesAudit()
    {
        // Arrange
        var (uow, urb, book) = CreateScenario(penalty: 250m);
        var paymentRepo = (FakeRepository<PenaltyPayment>)uow.GetRepository<PenaltyPayment>();
        var auditMock = new Mock<IAuditService>();
        var cashier = new FakeCurrentUserService { LibraryUserId = new Id(Guid.NewGuid()) };

        var command = new RegisterPenaltyPaymentCommand(uow, cashier, auditMock.Object);
        var request = new RegisterPenaltyPaymentRequest
        {
            UserRoomBookId = urb.Id.Value,
            Notes = "Квитанция №42"
        };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("250"));
            Assert.That(paymentRepo.Entities, Has.Count.EqualTo(1));

            var payment = paymentRepo.Entities.Single();
            Assert.That(payment.UserRoomBookId, Is.EqualTo(urb.Id));
            Assert.That(payment.Amount, Is.EqualTo(250m));
            Assert.That(payment.PaidByUserId, Is.EqualTo(cashier.LibraryUserId));
            Assert.That(payment.Notes, Is.EqualTo("Квитанция №42"));
        });

        // Аудит: тип Update, детали содержат сумму и название книги.
        // IsPenaltyPaid в фейке не пересчитывается (коллекция PenaltyPayments
        // не обновляется при AddAsync в paymentRepo), это проверяется смоуком.
        auditMock.Verify(a => a.LogAsync(
            AuditActionType.Update,
            nameof(PenaltyPayment),
            It.IsAny<Guid?>(),
            It.Is<string>(s => s.Contains("Принята оплата штрафа") && s.Contains(book.Title)),
            It.IsAny<Id?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Execute_WhenSaveFails_Throws()
    {
        // Arrange
        var (uow, urb, _) = CreateScenario(penalty: 100m);
        uow.SaveException = new Exception("DB error");
        var auditMock = new Mock<IAuditService>();

        var command = new RegisterPenaltyPaymentCommand(
            uow, new FakeCurrentUserService(), auditMock.Object);
        var request = new RegisterPenaltyPaymentRequest { UserRoomBookId = urb.Id.Value };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            command.Execute(request, CancellationToken.None));
        Assert.That(ex.Message, Is.EqualTo("DB error"));

        // LogAsync вызывается до SaveChanges (в одном action()), откат гарантирует,
        // что запись аудита не попадёт в БД. Verify(Never) некорректен — не проверяем.
    }
}