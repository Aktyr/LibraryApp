namespace BlazorWeb.Client.Services.Interfaces;

public interface IPenaltySettingsService
{
    Task<PenaltySettingsDTO> GetAsync(CancellationToken ct = default);
}