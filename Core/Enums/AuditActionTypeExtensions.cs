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
        _ => type.ToString()
    };
}