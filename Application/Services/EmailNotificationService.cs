using ISmtpClient = LibApp.Application.Interfaces.ISmtpClient;

namespace LibApp.Application.Services;

public class EmailNotificationService(
    ILogger<EmailNotificationService> logger,
    EmailValidatorAsync validator,
    IOptions<NotificationSettings> settings,
    ISmtpClient smtpClient) : INotificationService, IService
{
    // settings.Value кешируем: IOptions.Value — стабильная ссылка на конфигурацию,
    // но чтение через свойство на каждом Log-вызове создаёт лишний шум.
    private readonly NotificationSettings _settings = settings.Value;

    public async Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Валидация Email
        var validationResult = await validator.ValidateAsync(email, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        if (!_settings.Enabled)
        {
            logger.LogInformation($"[EMAIL DISABLED] To: {email}, Subject: {subject}\n{body}");
            return;
        }

        if (string.IsNullOrEmpty(_settings.SmtpServer) || string.IsNullOrEmpty(_settings.SmtpUsername))
        {
            logger.LogWarning("SMTP settings are incomplete. Email not sent.");
            return;
        }

        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            logger.LogInformation($"Email sent successfully to {email}: {subject}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to send email to {email}");
            throw;
        }
    }

    public async Task SendBorrowConfirmationAsync(User user, UserRoomBook userRoomBook, CancellationToken cancellationToken)
    {
        var subject = "Книга выдана";
        var body = $"""
            Здравствуйте, {user.FullName}!
            
            Вам выдана книга: "{userRoomBook.RoomBook.Book.Title}"
            Автор: {userRoomBook.RoomBook.Book.Author}
            Дата возврата: {userRoomBook.Deadline:d}
            
            С уважением, Библиотека.
            """;

        await SendEmailAsync(user.Email, subject, body, cancellationToken);
    }

    public async Task SendReturnReminderAsync(User user, UserRoomBook userRoomBook, CancellationToken cancellationToken)
    {
        var daysLeft = (userRoomBook.Deadline!.Value - DateTime.UtcNow).Days;
        var subject = $"Напоминание о возврате книги (осталось {daysLeft} дн.)";
        var body = $"""
            Здравствуйте, {user.FullName}!
            
            Напоминаем, что срок возврата книги "{userRoomBook.RoomBook.Book.Title}" истекает через {daysLeft} дней.
            Дата возврата: {userRoomBook.Deadline:d}
            
            Пожалуйста, верните книгу вовремя.
            
            С уважением, Библиотека.
            """;

        await SendEmailAsync(user.Email, subject, body, cancellationToken);
    }

    public async Task SendOverdueNotificationAsync(User user, UserRoomBook userRoomBook, decimal penalty, CancellationToken cancellationToken)
    {
        var daysOverdue = (DateTime.UtcNow - userRoomBook.Deadline!.Value).Days;
        var subject = "Просрочка возврата книги";
        var body = $"""
            Здравствуйте, {user.FullName}!
            
            Срок возврата книги "{userRoomBook.RoomBook.Book.Title}" истек {daysOverdue} дней назад.
            Сумма штрафа: {penalty} руб.
            
            Пожалуйста, верните книгу как можно скорее.
            
            С уважением, Библиотека.
            """;

        await SendEmailAsync(user.Email, subject, body, cancellationToken);
    }
    public async Task SendBorrowRequestApprovedAsync(
        User user,
        BorrowRequest borrowRequest,
        IReadOnlyCollection<string> warnings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(borrowRequest);
        ArgumentNullException.ThrowIfNull(warnings);

        // ExpiresAt заполняется только при Approve. Если сюда попала заявка без него —
        // нарушен инвариант статуса, письмо «заберите до null» бессмысленно.
        var expiresAt = borrowRequest.ExpiresAt
            ?? throw new InvalidOperationException(
                $"Approved borrow request {borrowRequest.Id.Value} has no ExpiresAt");

        var bookTitle = borrowRequest.RoomBook.Book.Title;
        var subject = "Заявка на книгу одобрена";

        var body = new StringBuilder();
        body.AppendLine($"Здравствуйте, {user.FullName}!");
        body.AppendLine();
        body.AppendLine($"Ваша заявка на книгу \"{bookTitle}\" одобрена.");
        body.AppendLine($"Заберите книгу до {expiresAt:dd.MM.yyyy HH:mm}.");
        body.AppendLine();

        if (warnings.Count > 0)
        {
            body.AppendLine("Обратите внимание:");
            foreach (var warning in warnings)
                body.AppendLine($"- {warning}");
            body.AppendLine();
        }

        body.AppendLine("С уважением, Библиотека.");

        await SendEmailAsync(user.Email, subject, body.ToString(), cancellationToken);
    }
}
