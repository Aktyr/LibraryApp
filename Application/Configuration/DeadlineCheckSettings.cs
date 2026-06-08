namespace LibApp.Application.Configuration;

public class DeadlineCheckSettings : ISettings
{
    [PositiveNumber]
    public int ReturnReminderInDays { get; set; } = 3;
    [PositiveNumber]
    public int CheckIntervalInHours { get; set; } = 24;
}
