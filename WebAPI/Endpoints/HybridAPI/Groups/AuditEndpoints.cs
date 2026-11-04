namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Audit")]
[Route("/api/audit")]
[EnumAuthorize(UserRole.Admin)]
public class AuditEndpoints : EndpointBase
{
    [HttpGet]
    public async Task<IResult> GetAuditLog(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] AuditActionType? actionType,
        [FromQuery] Guid? userId,
        [FromQuery] string? entityType,
        GetAuditLogCommand command,
        CancellationToken ct)
    {
        var request = new GetAuditLogRequest
        {
            FromDate = fromDate,
            ToDate = toDate,
            ActionType = actionType,
            UserId = userId,
            EntityType = entityType
        };
        return Results.Ok(await command.Execute(request, ct));
    }
}