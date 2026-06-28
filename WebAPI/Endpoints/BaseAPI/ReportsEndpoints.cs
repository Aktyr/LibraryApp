namespace WebAPI.Endpoints.Controllers;

[Route("api/reports")]
[EnumAuthorize(UserRole.Admin, UserRole.Librarian)]
public class ReportsEndpoints : BaseApiController
{
    public ReportsEndpoints(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [HttpGet("popular-books")]
    public async Task<ActionResult<BookPopularityReportResponse>> GetPopularBooks(
        [FromQuery] int? topCount = 10,
        CancellationToken ct = default)
    {
        var request = new GetPopularBooksRequest { TopCount = topCount };
        var result = await ExecuteQuery<GetPopularBooksReportCommand, GetPopularBooksRequest, BookPopularityReportResponse>(request, ct);
        return Ok(result);
    }

    [HttpGet("user-activity")]
    public async Task<ActionResult<UserActivityReportResponse>> GetUserActivity(
        [FromQuery] bool onlyWithOverdue = false,
        [FromQuery] bool onlyActive = false,
        CancellationToken ct = default)
    {
        var request = new GetUserActivityRequest { OnlyWithOverdue = onlyWithOverdue, OnlyActive = onlyActive };
        var result = await ExecuteQuery<GetUserActivityReportCommand, GetUserActivityRequest, UserActivityReportResponse>(request, ct);
        return Ok(result);
    }
}
