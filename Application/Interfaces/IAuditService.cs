namespace LibApp.Application.Interfaces;

/// <summary>Сервис журналирования действий пользователей.</summary>
public interface IAuditService
{
    /// <summary>
    /// Записывает действие в журнал аудита и добавляет его в текущую транзакцию.
    /// Вызывающая сторона обязана выполнить <c>IUnitOfWork.SaveChangesAsync()</c> для фиксации.
    /// </summary>
    /// <param name="action">Тип действия.</param>
    /// <param name="entityType">Имя сущности ("Book", "User").</param>
    /// <param name="entityId">Идентификатор сущности (может быть null).</param>
    /// <param name="details">Детали (JSON или текст).</param>
    /// <param name="userId">Явный ID пользователя. Используется при Login, когда <c>ICurrentUserService</c> ещё пуст.</param>
    /// <param name="userName">Явное ФИО пользователя. Используется при Login.</param>
    /// <param name="ct">Токен отмены.</param>
    Task LogAsync(
        AuditActionType action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        Id? userId = null,
        string? userName = null,
        CancellationToken ct = default);
}