namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Search")]
[Route("/api/search")]
[AllowAnonymous]
public class SearchEndpoints : EndpointBase
{
    [HttpGet("/books")]
    public async Task<IResult> SearchBooks(
        [FromQuery] string? query,
        [FromQuery] string? title,
        [FromQuery] string? author,
        [FromQuery] string? genre,
        [FromQuery] int? yearFrom,
        [FromQuery] int? yearTo,
        [FromQuery] string? publisher,
        [FromQuery] bool? availableOnly,
        [FromQuery] string? sortBy,
        [FromQuery] bool? sortDescending,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        SearchBooksCommand command,
        CancellationToken ct)
    {
        var request = new SearchBooksRequest
        {
            Query = query,
            Title = title,
            Author = author,
            Genre = genre,
            YearFrom = yearFrom,
            YearTo = yearTo,
            Publisher = publisher,
            AvailableOnly = availableOnly ?? false,
            SortBy = sortBy ?? "Title",
            SortDescending = sortDescending ?? false,
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 20
        };
        return Results.Ok(await command.Execute(request, ct));
    }
}