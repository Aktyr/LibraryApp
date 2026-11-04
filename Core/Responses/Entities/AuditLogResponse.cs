namespace LibApp.Core.Responses.Entities;

public record AuditLogResponse(string Status, string Message, AuditLogDTO[] Data) : IGetResponse;