namespace LibApp.ApplicationTests.Commands.Entities.Users;

[TestFixture]
public class ExtendDeadlineCommandTests
{
    private BorrowingValidatorAsync CreateValidator(BorrowingSettings? settings = null)
    {
        var optionsMock = new Mock<IOptionsSnapshot<BorrowingSettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings ?? new BorrowingSettings { MaxExtendDeadlineDays = 14 });
        return new BorrowingValidatorAsync(optionsMock.Object);
    }

    [Test]
    public async Task Execute_WhenUserRoomBookNotFound_ThrowsUserRoomBookNotFoundException()
    {
        // Arrange
        var userRoomBookRepo = new FakeRepository<UserRoomBook>();
        var command = new ExtendDeadlineCommand(userRoomBookRepo, CreateValidator());
        var request = new ExtendDeadlineRequest { UserRoomBookId = Guid.NewGuid(), ExtraDays = 7 };

        // Act & Assert
        Assert.ThrowsAsync<UserRoomBookNotFoundException>(() =>
            command.Execute(request, CancellationToken.None));
    }

    [Test]
    public async Task Execute_WhenValidationFails_ThrowsLibValidationException()
    {
        // Arrange
        var userRoomBookRepo = new FakeRepository<UserRoomBook>();
        var settings = new BorrowingSettings { MaxExtendDeadlineDays = 14 };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = DateTime.Now.AddDays(-5),
            Deadline = DateTime.Now.AddDays(5),
            ReturnDate = null // Книга на руках
        };
        await userRoomBookRepo.AddRange([userRoomBook]);

        var command = new ExtendDeadlineCommand(userRoomBookRepo, CreateValidator(settings));

        // Пытаемся продлить на 20 дней (больше максимума в 14 дней)
        var request = new ExtendDeadlineRequest { UserRoomBookId = userRoomBook.Id.Value, ExtraDays = 20 };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));

        Assert.That(ex.ExceptionDetails, Contains.Item("Срок продления должен быть от 1 до 14 дней"));
    }

    [Test]
    public async Task Execute_WithValidData_ExtendsDeadlineSuccessfully()
    {
        // Arrange
        var userRoomBookRepo = new FakeRepository<UserRoomBook>();
        var initialDeadline = DateTime.Now.AddDays(5);

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = DateTime.Now.AddDays(-5),
            Deadline = initialDeadline,
            ReturnDate = null
        };
        await userRoomBookRepo.AddRange([userRoomBook]);

        var command = new ExtendDeadlineCommand(userRoomBookRepo, CreateValidator());
        var request = new ExtendDeadlineRequest { UserRoomBookId = userRoomBook.Id.Value, ExtraDays = 7 };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Срок продлен до"));

            var updatedUrb = userRoomBookRepo.Entities.First();
            Assert.That(updatedUrb.Deadline, Is.EqualTo(initialDeadline.AddDays(7)).Within(TimeSpan.FromSeconds(2)));
        });
    }
    [Test]
    public async Task Execute_WhenExtraDaysIsZero_ThrowsValidationException()
    {
        // Arrange
        var userRoomBookRepo = new FakeRepository<UserRoomBook>();
        var settings = new BorrowingSettings { MaxExtendDeadlineDays = 14 };

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = new DateTime(2024, 1, 1, 10, 0, 0),
            Deadline = new DateTime(2024, 1, 15, 10, 0, 0),
            ReturnDate = null
        };
        await userRoomBookRepo.AddRange([userRoomBook]);

        var command = new ExtendDeadlineCommand(userRoomBookRepo, CreateValidator(settings));
        var request = new ExtendDeadlineRequest { UserRoomBookId = userRoomBook.Id.Value, ExtraDays = 0 };

        // Act & Assert
        var ex = Assert.ThrowsAsync<LibValidationException>(() =>
            command.Execute(request, CancellationToken.None));

        Assert.That(ex.ExceptionDetails, Contains.Item("Срок продления должен быть от 1 до 14 дней"));
    }

    [Test]
    public async Task Execute_WithValidData_DeadlineExtendedCorrectly()
    {
        // Arrange
        var userRoomBookRepo = new FakeRepository<UserRoomBook>();
        var initialDeadline = new DateTime(2024, 1, 15, 10, 0, 0);

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            BorrowDate = initialDeadline.AddDays(-14),
            Deadline = initialDeadline,
            ReturnDate = null
        };
        await userRoomBookRepo.AddRange([userRoomBook]);

        var command = new ExtendDeadlineCommand(userRoomBookRepo, CreateValidator());
        var request = new ExtendDeadlineRequest { UserRoomBookId = userRoomBook.Id.Value, ExtraDays = 7 };

        // Act
        var response = await command.Execute(request, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Ok"));
            Assert.That(response.Message, Does.Contain("Срок продлен до"));

            var updatedUrb = userRoomBookRepo.Entities.First();
            Assert.That(updatedUrb.Deadline, Is.EqualTo(initialDeadline.AddDays(7)));
        });
    }

}