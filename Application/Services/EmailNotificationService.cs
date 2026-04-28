namespace LibApp.Application.Services;

public class EmailNotificationService : INotificationService, IService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly EmailValidatorAsync _validator;
    public EmailNotificationService(ILogger<EmailNotificationService> logger, EmailValidatorAsync validator) 
    { 
        _logger = logger;
        _validator = validator;
    }


    public async Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        // todo: Реальная отправка email через SMTP или внешний сервис

        // Валидация Email
        var validationResult = await _validator.ValidateAsync(email, cancellationToken);
        if (!validationResult.IsValid)
            throw new Core.Exceptions.LibValidationException { ExceptionDetails = validationResult.Errors };

        _logger.LogInformation($"Email sent to {email}: {subject}\n{body}");
        await Task.CompletedTask;
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
        var daysLeft = (userRoomBook.Deadline!.Value - DateTime.Now).Days;
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
        var daysOverdue = (DateTime.Now - userRoomBook.Deadline!.Value).Days;
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
