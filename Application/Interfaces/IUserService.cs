namespace LibApp.Application.Interfaces;

public interface IUserService
{
    Task<User> CreateUserAsync(
        string email,
        string password,
        string lastName,
        string firstName,
        string middleName,
        string contactInfo,
        UserRole role,
        CancellationToken cancellationToken = default);
}