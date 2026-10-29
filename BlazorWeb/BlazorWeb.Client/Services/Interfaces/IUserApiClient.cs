using LibApp.Core.Enums;

namespace BlazorWeb.Client.Services.Interfaces;

public interface IUserApiClient
{
    Task<UserResponse> GetAllUsersAsync(CancellationToken ct = default);
    Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<BorrowResponse> GetUserBooksAsync(Guid id, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> UpdateUserAsync(UpdateUserRequest request, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> DeleteUserAsync(Guid id, CancellationToken ct = default);
    Task<BasicCreateDeleteResponse> ChangeStatusAsync(Guid id, SubscriptionStatus newStatus, CancellationToken ct = default);

}