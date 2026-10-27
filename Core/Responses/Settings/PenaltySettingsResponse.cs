namespace LibApp.Core.Responses.Settings;

public record PenaltySettingsResponse(string Status, string Message, PenaltySettingsDTO Data) : IGetResponse;