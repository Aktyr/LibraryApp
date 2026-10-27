namespace BlazorWeb.Client.Services.APIs;

public interface IBorrowingSettingsService
{
    Task<BorrowingSettingsDTO> GetAsync(CancellationToken ct = default);
}
