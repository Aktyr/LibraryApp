namespace BlazorWeb.Client.Services.APIs;

public class BorrowingSettingsService(HttpClient http) : IBorrowingSettingsService
{
    private BorrowingSettingsDTO? _cached;
    private Task<BorrowingSettingsDTO>? _loadTask;

    public Task<BorrowingSettingsDTO> GetAsync(CancellationToken ct = default)
    {
        if (_cached is not null) return Task.FromResult(_cached);
        return _loadTask ??= LoadInternalAsync(ct);
    }

    private async Task<BorrowingSettingsDTO> LoadInternalAsync(CancellationToken ct)
    {
        try
        {
            var resp = await http.GetFromJsonAsync<BorrowingSettingsResponse>(
                "/api/proxy/settings/borrowing", ct);

            _cached = resp?.Data ?? Defaults();
        }
        catch
        {
            // Если сеть/бэкенд временно недоступны — не ломаем UI.
            _cached = Defaults();
        }
        return _cached;
    }

    private static BorrowingSettingsDTO Defaults() => new(
        MaxBooksPerUser: 5,
        MaxExtendDeadlineDays: 14,
        MinBorrowDays: 1,
        MaxBorrowDays: 30);
}
