using LibApp.Core.DTO.Settings;

namespace LibApp.Core.Responses.Settings;

public record BorrowingSettingsResponse(string Status, string Message, BorrowingSettingsDTO Data) : IGetResponse;