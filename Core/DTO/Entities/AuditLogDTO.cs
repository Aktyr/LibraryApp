namespace LibApp.Core.DTO.Entities;

public record AuditLogDTO(
    Guid Id,
    DateTime Timestamp,
    Guid? UserId,
    string UserName,
    AuditActionType ActionType,
    string EntityType,
    Guid? EntityId,
    string? Details)
{
    public AuditLogDTO() : this(
        default, default, default, default!,
        default, default!, default, default)
    { }
}