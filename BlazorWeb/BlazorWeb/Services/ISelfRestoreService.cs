namespace BlazorWeb.Services;

/// <summary>
/// Самостоятельное восстановление аккаунта в BFF. В отличие от <c>SelfRestoreCommand</c> (WebAPI),
/// который только снимает soft-delete в library, здесь создаётся заново Identity-пользователь:
/// при Admin-удалении через <c>DeleteAsync</c> он был удалён физически.
/// </summary>
public interface ISelfRestoreService
{
    /// <exception cref="UnauthorizedException">Неверный email или пароль.</exception>
    /// <exception cref="LibValidationException">Аккаунт не в состоянии self-restore, или Identity-создание не удалось.</exception>
    Task SelfRestoreAsync(string email, string password, CancellationToken ct = default);
}