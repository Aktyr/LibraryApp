namespace BlazorWeb.Client.Services.APIs;

public class PenaltySettingsService(HttpClient http) : IPenaltySettingsService
{
    private PenaltySettingsDTO? _cached;
    private Task<PenaltySettingsDTO>? _loadTask;

    public Task<PenaltySettingsDTO> GetAsync(CancellationToken ct = default)
    {
        if (_cached is not null) return Task.FromResult(_cached);
        return _loadTask ??= LoadInternalAsync(ct);
    }

    private async Task<PenaltySettingsDTO> LoadInternalAsync(CancellationToken ct)
    {
        try
        {
            var resp = await http.GetFromJsonAsync<PenaltySettingsResponse>(
                "/api/proxy/settings/penalty", ct);

            _cached = resp?.Data ?? Defaults();
        }
        catch
        {
            _cached = Defaults();
        }
        return _cached;
    }

    private static PenaltySettingsDTO Defaults() => new(
        DailyRate: 10m,
        MaxPenalty: 500m,
        GracePeriodDays: 0);
}
