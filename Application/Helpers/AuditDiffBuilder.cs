namespace LibApp.Application.Helpers;

/// <summary>Хелпер для формирования diff-строк для журнала аудита.</summary>
public static class AuditDiffBuilder
{
    private const string EmptyValue = "(пусто)";
    /// <summary>
    /// Формирует строку изменения для журнала аудита.
    /// Возвращает пустую строку, если значения равны.
    /// </summary>
    /// <param name="field">Название поля (например, "Изменён автор").</param>
    /// <param name="oldValue">Старое значение.</param>
    /// <param name="newValue">Новое значение.</param>
    public static string FormatChange(string field, object? oldValue, object? newValue)
    {
        var oldStr = FormatValue(oldValue);
        var newStr = FormatValue(newValue);

        return oldStr == newStr ? string.Empty : $"{field}: {oldStr} → {newStr}";
    }

    private static string FormatValue(object? value) => value switch
    {
        null => EmptyValue,
        string s when string.IsNullOrEmpty(s) => EmptyValue,
        _ => value.ToString() ?? EmptyValue
    };
}