namespace LibApp.Core.Configuration;

public class ApplicationSettings
{
    public JwtSettings Jwt { get; set; } = new();
    public BorrowingSettings Borrowing { get; set; } = new();
    public PenaltySettings Penalty { get; set; } = new();
    public NotificationSettings Notification { get; set; } = new();
    public DeadlineCheckSettings DeadlineCheck { get; set; } = new();
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "LibraryApp";
    public string Audience { get; set; } = "LibraryApp";
    public int ExpiryMinutes { get; set; } = 60;
}

public class BorrowingSettings
{
    public int MaxBooksPerUser { get; set; } = 5;
    public int MinBorrowDays { get; set; } = 1;
    public int MaxBorrowDays { get; set; } = 30;
    public int MaxExtendDeadlineDays { get; set; } = 14;
}

public class PenaltySettings
{
    public decimal DailyRate { get; set; } = 10;
    public decimal? MaxPenalty { get; set; } = 500;
    public int GracePeriodDays { get; set; } = 0;
    public void Validate() // todo вынести в отдельный валидатор
    {
        if (DailyRate <= 0)
            throw new InvalidOperationException("DailyRate must be greater than 0");

        if (MaxPenalty.HasValue && MaxPenalty.Value <= 0)
            throw new InvalidOperationException("MaxPenalty must be greater than 0");

        if (GracePeriodDays < 0)
            throw new InvalidOperationException("GracePeriodDays cannot be negative");
    }

}

public class NotificationSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class DeadlineCheckSettings
{
    public int ReturnReminderInDays { get; set; } = 3;
    public int CheckIntervalInHours { get; set; } = 24;
}