namespace LibApp.Core.Requests.Auth;

/// <summary>
/// Запрос на самостоятельное восстановление удалённого аккаунта.
/// Пароль перепроверяется: удалённый пользователь не имеет токена, credentials — единственный фактор.
/// </summary>
public class SelfRestoreRequest : IAddOrUpdateRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}