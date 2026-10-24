namespace BlazorWeb.Client.Services;

public interface IBookApiClient
{
    Task<BookResponse> GetAllBooksAsync(CancellationToken ct = default);
    Task<BookResponse> GetBookByIdAsync(Guid id, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> CreateBookAsync(CreateBookRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> UpdateBookAsync(UpdateBookRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> DeleteBookAsync(Guid id, CancellationToken ct = default);
}