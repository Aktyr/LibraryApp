namespace LibApp.Application.Interfaces;

/// <summary>Сервис email-уведомлений читателям. Сбои SMTP — ответственность вызывающей стороны:
/// сервис пробрасывает исключения, команды решают, откатывать транзакцию или логировать.</summary>
public interface INotificationService
{
    /// <summary>Отправляет произвольное письмо. Валидирует адрес через EmailPolicy.</summary>
    Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>Письмо «книга выдана»: ФИО, название, срок возврата. Отправляется после
    /// фактической выдачи (IssueBorrowRequestCommand, BorrowBookCommand).</summary>
    Task SendBorrowConfirmationAsync(User user, UserRoomBook userRoomBook, CancellationToken cancellationToken);

    /// <summary>Напоминание о приближении срока возврата.</summary>
    Task SendReturnReminderAsync(User user, UserRoomBook userRoomBook, CancellationToken cancellationToken);

    /// <summary>Уведомление о просрочке с суммой штрафа.</summary>
    Task SendOverdueNotificationAsync(User user, UserRoomBook userRoomBook, decimal penalty, CancellationToken cancellationToken);

    /// <summary>
    /// Письмо «заявка одобрена, заберите книгу до ExpiresAt». Отправляется при Approve,
    /// до фактической выдачи. <paramref name="warnings"/> — предупреждения читателю
    /// (просрочки, лимит книг на руках): не блокируют одобрение, но читатель должен
    /// знать, что бронь не даёт права забрать книгу, пока не вернёт текущие.
    /// </summary>
    Task SendBorrowRequestApprovedAsync(
        User user,
        BorrowRequest borrowRequest,
        IReadOnlyCollection<string> warnings,
        CancellationToken cancellationToken);
}