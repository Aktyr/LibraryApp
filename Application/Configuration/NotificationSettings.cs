namespace LibApp.Application.Configuration;

public class NotificationSettings : ISettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;

    [ValidEmail]
    public string FromEmail { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
