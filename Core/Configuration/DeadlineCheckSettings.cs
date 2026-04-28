namespace LibApp.Core.Configuration;

public class DeadlineCheckSettings : ISettings
{
    public int ReturnReminderInDays { get; set; } = 3;
    public int CheckIntervalInHours { get; set; } = 24;
}
