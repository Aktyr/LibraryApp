namespace LibApp.Application.Helpers;

/// <summary>
/// Помечает write-команду, которая не аудируется напрямую (делегирует в сервис).
/// Обязательный аргумент <paramref name="reason"/> — гарантия того, что пропуск аудита
/// осознанное решение, а не забывчивость. Architecture test в ApplicationTests
/// проверяет: каждая write-команда либо инжектит IAuditService, либо помечена этим атрибутом.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class NoAuditAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}