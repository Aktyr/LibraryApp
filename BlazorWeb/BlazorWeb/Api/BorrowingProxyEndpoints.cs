namespace BlazorWeb.Api;

public static class BorrowingProxyEndpoints
{
    public static void MapBorrowingProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/borrowing")
            .RequireAuthorization()
            .WithTags("Proxy");

        group.MapPost("/borrow", BorrowAsync);
        group.MapPost("/return", ReturnAsync);
        group.MapPost("/extend", ExtendAsync);
    }

    private static Task<IResult> BorrowAsync(
        BorrowBookRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post, "/api/borrowing/borrow", request, ct);

    private static Task<IResult> ReturnAsync(
        ReturnBookRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post, "/api/borrowing/return", request, ct);

    private static Task<IResult> ExtendAsync(
        ExtendDeadlineRequest request, IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Post, "/api/borrowing/extend", request, ct);
}