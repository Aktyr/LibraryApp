namespace LibApp.Application.Configuration;

public class PenaltySettings : ISettings
{
    [PositiveNumber]
    public decimal DailyRate { get; set; } = 10;


    [PositiveNumber]
    public decimal? MaxPenalty { get; set; } = 500;


    [MinRange(0)]
    public int GracePeriodDays { get; set; } = 0;
}
