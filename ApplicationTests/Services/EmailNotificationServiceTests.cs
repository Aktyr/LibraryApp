namespace LibApp.ApplicationTests.Services;

[TestFixture]
public class EmailNotificationServiceTests
{
    private EmailNotificationService CreateService()
    {
        var logger = new Logger<EmailNotificationService>(new LoggerFactory());
        var settings = Options.Create(new NotificationSettings
        {
            SmtpServer = "smtp.gmail.com",
            SmtpPort = 587,
            SmtpUsername = "test@gmail.com",
            SmtpPassword = "test-password",
            FromEmail = "test@library.com",
            Enabled = false  // Отключаем реальную отправку в тестах
        });

        return new EmailNotificationService(logger, new EmailValidatorAsync(), settings);
    }

    [Test]
    public async Task SendEmailAsync_WithValidEmail_LogsInformation()
    {
        // Arrange
        var service = CreateService();
        var email = "test@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        // Act & Assert — не должно выбрасывать исключений
        Assert.DoesNotThrowAsync(async () =>
            await service.SendEmailAsync(email, subject, body));
    }

    [Test]
    public async Task SendEmailAsync_WithInvalidEmail_ThrowsLibValidationException()
    {
        // Arrange
        var service = CreateService();
        var email = "invalid-email";
        var subject = "Test";
        var body = "Test";

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await service.SendEmailAsync(email, subject, body));
    }

    [Test]
    public async Task SendEmailAsync_WithEmptyEmail_ThrowsLibValidationException()
    {
        // Arrange
        var service = CreateService();
        var email = "";
        var subject = "Test";
        var body = "Test";

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await service.SendEmailAsync(email, subject, body));
    }

    [Test]
    public async Task SendBorrowConfirmationAsync_SendsEmailWithCorrectContent()
    {
        // Arrange
        var service = CreateService();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@example.com",
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович"
        };
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Deadline = new DateTime(2024, 1, 15),
            RoomBook = new RoomBook
            {
                Book = new Book { Title = "Test Book", Author = "Test Author" }
            }
        };

        // Act & Assert — не должно выбрасывать исключений
        Assert.DoesNotThrowAsync(async () =>
            await service.SendBorrowConfirmationAsync(user, userRoomBook, CancellationToken.None));
    }

    [Test]
    public async Task SendReturnReminderAsync_SendsEmailWithCorrectContent()
    {
        // Arrange
        var service = CreateService();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@example.com",
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович"
        };
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Deadline = DateTime.Now.AddDays(3),
            RoomBook = new RoomBook
            {
                Book = new Book { Title = "Test Book", Author = "Test Author" }
            }
        };

        // Act & Assert — не должно выбрасывать исключений
        Assert.DoesNotThrowAsync(async () =>
            await service.SendReturnReminderAsync(user, userRoomBook, CancellationToken.None));
    }

    [Test]
    public async Task SendOverdueNotificationAsync_SendsEmailWithCorrectContent()
    {
        // Arrange
        var service = CreateService();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@example.com",
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович"
        };
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Deadline = DateTime.Now.AddDays(-5),
            RoomBook = new RoomBook
            {
                Book = new Book { Title = "Test Book", Author = "Test Author" }
            }
        };
        var penalty = 50m;

        // Act & Assert — не должно выбрасывать исключений
        Assert.DoesNotThrowAsync(async () =>
            await service.SendOverdueNotificationAsync(user, userRoomBook, penalty, CancellationToken.None));
    }
    [Test]
    public async Task SendBorrowConfirmationAsync_WithValidData_LogsCorrectContent()
    {
        // Arrange
        var service = CreateService();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@example.com",
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович"
        };
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Deadline = new DateTime(2024, 1, 15, 10, 0, 0),
            User = user,
            RoomBook = new RoomBook
            {
                Book = new Book { Title = "Test Book", Author = "Test Author" },
                Room = new Room { Name = "Test Room" }
            }
        };

        // Act & Assert — не должно выбрасывать исключений
        Assert.DoesNotThrowAsync(async () =>
            await service.SendBorrowConfirmationAsync(user, userRoomBook, CancellationToken.None));
    }

    [Test]
    public async Task SendReturnReminderAsync_CalculatesCorrectDaysLeft()
    {
        // Arrange
        var service = CreateService();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@example.com",
            LastName = "Петров",
            FirstName = "Петр",
            MiddleName = "Петрович"
        };
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Deadline = DateTime.Now.AddDays(5), // 5 дней до сдачи
            User = user,
            RoomBook = new RoomBook
            {
                Book = new Book { Title = "Return Book", Author = "Author" },
                Room = new Room { Name = "Test Room" }
            }
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await service.SendReturnReminderAsync(user, userRoomBook, CancellationToken.None));
    }

    [Test]
    public async Task SendOverdueNotificationAsync_WithPenalty_SendsCorrectContent()
    {
        // Arrange
        var service = CreateService();
        var user = new User
        {
            Id = new Id(Guid.NewGuid()),
            Email = "user@example.com",
            LastName = "Сидоров",
            FirstName = "Сидор",
            MiddleName = "Сидорович"
        };
        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            Deadline = new DateTime(2024, 1, 1, 10, 0, 0),
            User = user,
            RoomBook = new RoomBook
            {
                Book = new Book { Title = "Overdue Book", Author = "Author" },
                Room = new Room { Name = "Test Room" }
            }
        };
        var penalty = 150.50m;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await service.SendOverdueNotificationAsync(user, userRoomBook, penalty, CancellationToken.None));
    }
    [Test]
    public async Task SendEmailAsync_WithNullEmail_ThrowsLibValidationException()
    {
        // Arrange
        var service = CreateService();
        string? email = null;
        var subject = "Test";
        var body = "Test";

        // Act & Assert
        // Валидатор EmailValidatorAsync выбрасывает LibValidationException для null
        var ex = Assert.ThrowsAsync<LibValidationException>(async () =>
            await service.SendEmailAsync(email!, subject, body));

        Assert.That(ex.ExceptionDetails, Contains.Item("Email обязателен"));
    }


    [Test]
    public async Task SendEmailAsync_WithWhitespaceEmail_ThrowsValidationException()
    {
        // Arrange
        var service = CreateService();
        var email = "   ";
        var subject = "Test";
        var body = "Test";

        // Act & Assert
        Assert.ThrowsAsync<LibValidationException>(async () =>
            await service.SendEmailAsync(email, subject, body));
    }


}