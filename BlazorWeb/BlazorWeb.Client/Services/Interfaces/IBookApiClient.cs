namespace BlazorWeb.Client.Services.Interfaces;

public interface IBookApiClient
{
    Task<BookResponse> GetAllBooksAsync(CancellationToken ct = default);
    Task<BookResponse> GetBookByIdAsync(Guid id, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> CreateBookAsync(CreateBookRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> UpdateBookAsync(UpdateBookRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> DeleteBookAsync(Guid id, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> DiscardBookAsync(DiscardBookRequest request, CancellationToken ct = default);
    Task<DiscardedBookResponse> GetDiscardedBooksAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        LibApp.Core.Enums.DiscardReason? discardReason = null,
        Guid? roomId = null,
        Guid? approvedById = null,
        Guid? bookId = null,
        CancellationToken ct = default);
}