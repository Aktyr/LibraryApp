namespace LibApp.Core.Configuration;

public class PenaltySettings : ISettings
{
    public decimal DailyRate { get; set; } = 10;
    public decimal? MaxPenalty { get; set; } = 500;
    public int GracePeriodDays { get; set; } = 0;
    public void Validate() // todo вынести в отдельный валидатор? использовать атрибуты как в JwtSettings?
    {
        if (DailyRate <= 0)
            throw new InvalidOperationException("DailyRate must be greater than 0");

        if (MaxPenalty.HasValue && MaxPenalty.Value <= 0)
            throw new InvalidOperationException("MaxPenalty must be greater than 0");

        if (GracePeriodDays < 0)
            throw new InvalidOperationException("GracePeriodDays cannot be negative");
    }

}
