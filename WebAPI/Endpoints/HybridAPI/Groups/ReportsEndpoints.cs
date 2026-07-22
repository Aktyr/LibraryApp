namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Reports")]
[Route("/api/reports")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class ReportsEndpoints : EndpointBase
{
    [HttpGet("/popular-books")]
    public IResult PopularBooks([FromQuery] int? topCount, GetPopularBooksReportCommand command, CancellationToken ct)
        => Results.Ok(command.Execute(new GetPopularBooksRequest { TopCount = topCount ?? 10 }, ct));

    [HttpGet("/user-activity")]
    public IResult UserActivity([FromQuery] bool onlyWithOverdue, [FromQuery] bool onlyActive, GetUserActivityReportCommand command, CancellationToken ct)
        => Results.Ok(command.Execute(new GetUserActivityRequest { OnlyWithOverdue = onlyWithOverdue, OnlyActive = onlyActive }, ct));
}
