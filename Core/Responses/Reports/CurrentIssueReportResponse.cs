namespace LibApp.Core.Responses.Reports;

/// <summary>Отчёт «Текущие выдачи»: книги на руках на момент запроса, включая выдачи деактивированных читателей.</summary>
public record CurrentIssueReportResponse(
    string Status,
    string Message,
    CurrentIssueReportDTO[] Data) : IGetResponse;