namespace LibApp.Core.DTO.Settings;

public record PenaltySettingsDTO(
    decimal DailyRate,
    decimal? MaxPenalty,
    int GracePeriodDays);