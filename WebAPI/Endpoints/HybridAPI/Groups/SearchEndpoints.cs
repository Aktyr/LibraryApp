namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Search")]
[Route("/api/search")]
[AllowAnonymous]
public class SearchEndpoints : EndpointBase
{
    [HttpGet("/books")]
    public async Task<IResult> SearchBooks([FromQuery] string? query, [FromQuery] string? title,
                               [FromQuery] string? author, [FromQuery] int? yearFrom,
                               [FromQuery] int? yearTo, [FromQuery] string? publisher,
                               [FromQuery] bool availableOnly, [FromQuery] string? sortBy,
                               [FromQuery] bool sortDescending, SearchBooksCommand command, CancellationToken ct)
    {
        var request = new SearchBooksRequest
        {
            Query = query,
            Title = title,
            Author = author,
            YearFrom = yearFrom,
            YearTo = yearTo,
            Publisher = publisher,
            AvailableOnly = availableOnly,
            SortBy = sortBy ?? "Title",
            SortDescending = sortDescending
        };
        return Results.Ok(command.Execute(request, ct));
    }
}
