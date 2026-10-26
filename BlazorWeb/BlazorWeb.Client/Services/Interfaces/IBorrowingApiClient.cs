namespace BlazorWeb.Client.Services.Interfaces;

public interface IBorrowingApiClient
{
    Task<BasicCreateDeleteResponse> BorrowBookAsync(BorrowBookRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> ReturnBookAsync(ReturnBookRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> ExtendDeadlineAsync(ExtendDeadlineRequest request, CancellationToken ct = default);
}