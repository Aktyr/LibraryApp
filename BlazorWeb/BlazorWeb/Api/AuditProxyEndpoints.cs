namespace BlazorWeb.Api;

public static class AuditProxyEndpoints
{
    public static void MapAuditProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/audit")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapGet("/", GetAuditLogAsync);
    }

    private static Task<IResult> GetAuditLogAsync(
        DateTime? fromDate, DateTime? toDate, AuditActionType? actionType,
        Guid? userId, string? entityType,
        IHttpClientFactory httpFactory, CancellationToken ct)
    {
        var q = new List<string>();
        if (fromDate.HasValue) q.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) q.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        if (actionType.HasValue) q.Add($"actionType={actionType.Value}");
        if (userId.HasValue) q.Add($"userId={userId.Value}");
        if (!string.IsNullOrWhiteSpace(entityType))
            q.Add($"entityType={Uri.EscapeDataString(entityType)}");

        var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
        return ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, $"/api/audit{qs}", null, ct);
    }
}