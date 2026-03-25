namespace LibApp.Application.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default);
    Task SendBorrowConfirmationAsync(User user, UserRoomBook userRoomBook, CancellationToken cancellationToken);
    Task SendReturnReminderAsync(User user, UserRoomBook userRoomBook, CancellationToken cancellationToken);
    Task SendOverdueNotificationAsync(User user, UserRoomBook userRoomBook, decimal penalty, CancellationToken cancellationToken);
}
