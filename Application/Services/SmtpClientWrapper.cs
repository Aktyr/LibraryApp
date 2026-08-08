namespace LibApp.Application.Services;

public class SmtpClientWrapper : ISmtpClient
{
    private readonly SmtpClient _client;

    public SmtpClientWrapper(IOptions<NotificationSettings> settings)
    {
        var cfg = settings.Value;
        _client = new SmtpClient(cfg.SmtpServer, cfg.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(cfg.SmtpUsername, cfg.SmtpPassword),
            Timeout = 30000
        };
    }

    public Task SendMailAsync(MailMessage message, CancellationToken cancellationToken = default)
        => _client.SendMailAsync(message, cancellationToken);

    public void Dispose() => _client.Dispose();
}