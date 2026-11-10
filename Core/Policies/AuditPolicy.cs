namespace LibApp.Core.Policies;

/// <summary>
/// Ограничения полей AuditLog. Синхронизированы с EF-конфигурацией (AuditLogConfiguration)
/// через те же константы. Сервисы аудита (Application и BFF) обрезают значения
/// по этим лимитам, чтобы SaveChanges не падал с Npgsql.PostgresException
/// на длинном Details или составном UserName.
/// </summary>
public static class AuditPolicy
{
    public const int UserNameMaxLength = 200;
    public const int EntityTypeMaxLength = 100;
    public const int DetailsMaxLength = 4000;

    /// <summary>Суффикс обрезанного значения — видно в журнале, что данные усечены.</summary>
    public const string TruncationSuffix = "… (truncated)";

    /// <summary>Обрезает строку до <paramref name="maxLength"/> символов с суффиксом.</summary>
    public static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        var cutAt = Math.Max(0, maxLength - TruncationSuffix.Length);
        return value[..cutAt] + TruncationSuffix;
    }
}