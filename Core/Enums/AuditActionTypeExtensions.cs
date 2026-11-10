namespace LibApp.Core.Enums;

public static class AuditActionTypeExtensions
{
    public static string DisplayName(this AuditActionType type) => type switch
    {
        AuditActionType.Create => "Создание",
        AuditActionType.Update => "Обновление",
        AuditActionType.Delete => "Удаление",
        AuditActionType.Restore => "Восстановление",
        AuditActionType.Login => "Вход",
        AuditActionType.Logout => "Выход",
        AuditActionType.Borrow => "Выдача",
        AuditActionType.Return => "Возврат",
        AuditActionType.Register => "Регистрация",
        AuditActionType.Discard => "Списание",
        AuditActionType.UndoDiscard => "Отмена списания",
        AuditActionType.Anonymize => "Анонимизация",
        AuditActionType.ApproveBorrowRequest => "Одобрение заявки",
        AuditActionType.RejectBorrowRequest => "Отклонение заявки",
        _ => type.ToString()
    };
}