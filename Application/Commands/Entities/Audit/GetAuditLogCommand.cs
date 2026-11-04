namespace LibApp.Application.Commands.Audit;

public class GetAuditLogCommand(IUnitOfWork unitOfWork)
    : IGetQuery<GetAuditLogRequest, AuditLogResponse>, ICommand
{
    public async Task<AuditLogResponse> Execute(GetAuditLogRequest request, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<AuditLog>();

        var from = request.FromDate.HasValue
            ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        // Если передана только дата (время 00:00:00), добавляем день, 
        // чтобы включить все записи за "ToDate"
        var to = request.ToDate.HasValue
            ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc).AddDays(1).AddTicks(-1)
            : (DateTime?)null;

        var raw = await repo.ExecuteQueryAsync(query =>
        {
            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value);

            if (request.ActionType.HasValue)
                query = query.Where(l => l.ActionType == request.ActionType.Value);

            if (request.UserId.HasValue)
            {
                var id = new Id(request.UserId.Value);
                query = query.Where(l => l.UserId == id);
            }

            if (!string.IsNullOrWhiteSpace(request.EntityType))
                query = query.Where(l => l.EntityType == request.EntityType);

            return query
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new
                {
                    Id = l.Id,
                    l.Timestamp,
                    l.UserId,
                    l.UserName,
                    l.ActionType,
                    l.EntityType,
                    l.EntityId,
                    l.Details
                });
        }, ct);

        var dtos = raw.Select(x => new AuditLogDTO(
            x.Id.Value,
            x.Timestamp,
            x.UserId?.Value,
            x.UserName,
            x.ActionType,
            x.EntityType,
            x.EntityId,
            x.Details)).ToArray();

        return ResponseFactory.Found<AuditLog, AuditLogDTO, AuditLogResponse>(dtos);
    }
}