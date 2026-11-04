namespace BlazorWeb.Client.Services.Interfaces;

public interface IAuditApiClient
{
    Task<AuditLogResponse> GetAuditLogAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        AuditActionType? actionType = null,
        Guid? userId = null,
        string? entityType = null,
        CancellationToken ct = default);
}