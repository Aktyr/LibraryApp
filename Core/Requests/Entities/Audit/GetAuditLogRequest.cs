namespace LibApp.Core.Requests.Entities.Audit;

public class GetAuditLogRequest : IGetRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public AuditActionType? ActionType { get; set; }
    public Guid? UserId { get; set; }
    public string? EntityType { get; set; }
}