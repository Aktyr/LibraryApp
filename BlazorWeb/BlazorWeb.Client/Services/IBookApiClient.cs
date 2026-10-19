namespace BlazorWeb.Client.Services;

public interface IBookApiClient
{
    Task<BookResponse> GetAllBooksAsync(CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> CreateBookAsync(CreateBookRequest request, CancellationToken ct = default);
}