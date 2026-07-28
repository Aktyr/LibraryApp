namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Reports")]
[Route("/api/reports")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class ReportsEndpoints : EndpointBase
{
    [HttpGet("/popular-books")]
    public async Task<IResult> PopularBooks([FromQuery] int? topCount, GetPopularBooksReportCommand command, CancellationToken ct)
        => Results.Ok(await command.Execute(new GetPopularBooksRequest { TopCount = topCount ?? 10 }, ct));

    [HttpGet("/user-activity")]
    public async Task<IResult> UserActivity([FromQuery] bool onlyWithOverdue, [FromQuery] bool onlyActive, GetUserActivityReportCommand command, CancellationToken ct)
        => Results.Ok(await command.Execute(new GetUserActivityRequest { OnlyWithOverdue = onlyWithOverdue, OnlyActive = onlyActive }, ct));
}
