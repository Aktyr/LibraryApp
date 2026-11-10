namespace LibApp.Application.Audit;

/// <summary>
/// Описание записи аудита, которую команда хочет зафиксировать.
/// Возвращается из <c>ExecuteCore</c> и записывается базовым классом в той же транзакции,
/// что и бизнес-изменения. <c>null</c> — команда не пишет аудит (делегирует в сервис).
/// </summary>
public sealed record AuditEntry(
    AuditActionType Action,
    string EntityType,
    Guid? EntityId,
    string? Details,
    Id? UserId = null,
    string? UserName = null);