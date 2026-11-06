namespace LibApp.Core.Responses.Auth;

/// <summary>
/// Ответ на попытку входа. При Status = "Ok" — Token заполнен. При Status = "RestoreRequired" —
/// Token = null, CanSelfRestore = true: аккаунт удалён самим пользователем, можно восстановить
/// через POST /api/auth/restore-account.
/// </summary>
public record LoginResponse(
    string Status,
    string Message,
    string? Token,
    string Email,
    UserRole Role,
    Guid UserId,
    bool CanSelfRestore = false) : IGetResponse;