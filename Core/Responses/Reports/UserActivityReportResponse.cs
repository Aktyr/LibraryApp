namespace LibApp.Core.Responses.Reports;

public record UserActivityReportResponse(
    string Status,
    string Message,
    UserActivityReportDTO[] Data
) : IGetResponse;
