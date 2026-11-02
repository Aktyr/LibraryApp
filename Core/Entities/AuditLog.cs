namespace LibApp.Core.Entities;

/// <summary>
/// Запись журнала аудита. Хранит снимок состояния на момент действия,
/// чтобы данные оставались читаемыми даже после удаления связанных сущностей.
/// </summary>
public class AuditLog : IEntity
{
    public Id Id { get; set; }

    /// <summary>Время совершения действия в UTC.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Идентификатор пользователя, совершившего действие. Nullable, так как пользователь может быть удалён.</summary>
    public Id? UserId { get; set; }

    /// <summary>Снимок ФИО пользователя на момент действия. Не меняется при последующем редактировании пользователя.</summary>
    public string UserName { get; set; } = string.Empty;

    public AuditActionType ActionType { get; set; }

    /// <summary>Имя сущности, к которой применено действие (например, "Book", "User").</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Идентификатор изменённой сущности. Nullable для действий без конкретной цели (например, Login).</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Дополнительные сведения в формате JSON или свободный текст (например, список изменённых полей).</summary>
    public string? Details { get; set; }
}