namespace LibApp.Application.Interfaces;

public interface ICurrentUserService
{
    Id? LibraryUserId { get; }
    string? FullName { get; }
    string? Email { get; }
    UserRole? Role { get; }
}