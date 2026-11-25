namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Reports")]
[Route("/api/reports")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class ReportsEndpoints : EndpointBase
{
    [HttpGet("/popular-books")]
    public async Task<IResult> PopularBooks([FromQuery] int? topCount, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, GetPopularBooksReportCommand command, CancellationToken ct)
        => Results.Ok(await command.Execute(new GetPopularBooksRequest { TopCount = topCount, FromDate = fromDate, ToDate = toDate }, ct));

    [HttpGet("/user-activity")]
    public async Task<IResult> UserActivity([FromQuery] bool onlyWithOverdue, [FromQuery] bool onlyActive, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, GetUserActivityReportCommand command, CancellationToken ct)
        => Results.Ok(await command.Execute(new GetUserActivityRequest { OnlyWithOverdue = onlyWithOverdue, OnlyActive = onlyActive, FromDate = fromDate, ToDate = toDate }, ct));

    /// <summary>Книги на руках сейчас, включая выдачи деактивированных читателей. Фильтры применяет клиент, поэтому параметров нет.</summary>
    [HttpGet("/current-issues")]
    public async Task<IResult> CurrentIssues(GetCurrentIssuesReportCommand command, CancellationToken ct)
        => Results.Ok(await command.Execute(new EmptyRequest(), ct));
}
