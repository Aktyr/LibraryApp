namespace LibApp.Application.Interfaces;

public interface ISmtpClient : IDisposable
{
    Task SendMailAsync(MailMessage message, CancellationToken cancellationToken = default);
}