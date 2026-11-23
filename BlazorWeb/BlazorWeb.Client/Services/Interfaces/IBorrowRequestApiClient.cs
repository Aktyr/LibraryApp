namespace BlazorWeb.Client.Services.Interfaces;

public interface IBorrowRequestApiClient
{
    Task<BasicCreateDeleteResponse> CreateAsync(
        CreateBorrowRequest request, CancellationToken ct = default);
    Task<BorrowRequestResponse> GetMyAsync(
        GetBorrowRequestsRequest request, CancellationToken ct = default);
    Task<BorrowRequestResponse> GetAllAsync(
        GetBorrowRequestsRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> ApproveAsync(
        Guid borrowRequestId, ApproveBorrowRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> RejectAsync(
        Guid borrowRequestId, RejectBorrowRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> IssueAsync(
        Guid borrowRequestId, IssueBorrowRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> CancelAsync(
        Guid borrowRequestId, CancelBorrowRequest request, CancellationToken ct = default);

    /// <summary>Количество заявок в указанном статусе. Использует pageSize=1 для минимального трафика.</summary>
    Task<int> GetCountByStatusAsync(BorrowRequestStatus status, CancellationToken ct = default);
}