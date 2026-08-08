namespace LibApp.Application.Services;

public class EmailNotificationService(
    ILogger<EmailNotificationService> logger,
    EmailValidatorAsync validator,
    IOptions<NotificationSettings> settings,
    ISmtpClient smtpClient) : INotificationService, IService
{
    private readonly ILogger<EmailNotificationService> _logger = logger;
    private readonly EmailValidatorAsync _validator = validator;
    private readonly NotificationSettings _settings = settings.Value;
    private readonly ISmtpClient _smtpClient = smtpClient;

    public async Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Валидация Email
        var validationResult = await _validator.ValidateAsync(email, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        if (!_settings.Enabled)
        {
            _logger.LogInformation($"[EMAIL DISABLED] To: {email}, Subject: {subject}\n{body}");
            return;
        }

        if (string.IsNullOrEmpty(_settings.SmtpServer) || string.IsNullOrEmpty(_settings.SmtpUsername))
        {
            _logger.LogWarning("SMTP settings are incomplete. Email not sent.");
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

            await _smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation($"Email sent successfully to {email}: {subject}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {email}");
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

}
