namespace BlazorWeb.Client.Services.APIs;

public class AuditApiClient(HttpClient http) : IAuditApiClient
{
    private const string BasePath = "/api/proxy/audit";

    public async Task<AuditLogResponse> GetAuditLogAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        AuditActionType? actionType = null,
        Guid? userId = null,
        string? entityType = null,
        CancellationToken ct = default)
    {
        var q = new List<string>();
        if (fromDate.HasValue) q.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) q.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        if (actionType.HasValue) q.Add($"actionType={actionType.Value}");
        if (userId.HasValue) q.Add($"userId={userId.Value}");
        if (!string.IsNullOrWhiteSpace(entityType))
            q.Add($"entityType={Uri.EscapeDataString(entityType)}");

        var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
        return await http.GetFromJsonAsync<AuditLogResponse>($"{BasePath}{qs}", ct)
               ?? new AuditLogResponse("Error", "Empty response", []);
    }
}