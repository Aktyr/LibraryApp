namespace LibApp.Core.Responses.Reports;

public record BookPopularityReportResponse(
    string Status,
    string Message,
    BookPopularityReportDTO[] Data
) : IGetResponse;
