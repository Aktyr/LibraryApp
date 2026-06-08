using WebAPI.Endpoints.Service;

namespace WebAPI.Endpoints;

[Route("api/search")]
public class SearchEndpoints : BaseEndpoint
{
    public SearchEndpoints(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [HttpGet("books")]
    public async Task<ActionResult<BookResponse>> SearchBooks(
        [FromQuery] string? query,
        [FromQuery] string? title,
        [FromQuery] string? author,
        [FromQuery] int? yearFrom,
        [FromQuery] int? yearTo,
        [FromQuery] string? publisher,
        [FromQuery] bool availableOnly = false,
        [FromQuery] string? sortBy = "Title",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
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
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await ExecuteQuery<SearchBooksCommand, SearchBooksRequest, BookResponse>(
            request, cancellationToken);
        return Ok(result);
    }
}