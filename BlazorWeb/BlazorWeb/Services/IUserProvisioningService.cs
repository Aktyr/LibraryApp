using LibApp.Core.Enums;

namespace BlazorWeb.Services;

/// <summary>
/// Единая точка создания пары «Identity ApplicationUser + library User».
/// Отвечает за привязку <c>LibraryUserId</c> и компенсацию (rollback Identity),
/// если создание library-пользователя провалилось.
/// </summary>
public interface IUserProvisioningService
{
    /// <param name="emailConfirmed">
    /// true для seed-админа и admin-панели (пользователя создаёт сотрудник),
    /// false для самостоятельной регистрации (требуется подтверждение email).
    /// </param>
    /// <exception cref="ProvisioningException">
    /// Ошибка Identity (пароль/дубль email), Library (валидатор) или неизвестная.
    /// </exception>
    Task<ApplicationUser> CreateAsync(
        string email,
        string password,
        string lastName,
        string firstName,
        string middleName,
        string contactInfo,
        UserRole role,
        bool emailConfirmed = true,
        CancellationToken ct = default);
}