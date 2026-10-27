namespace BlazorWeb.Api;

public static class SettingsProxyEndpoints
{
    public static void MapSettingsProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/settings")
            .WithTags("Proxy");

        group.MapGet("/borrowing", GetBorrowingAsync);
        group.MapGet("/penalty", GetPenaltyAsync);
    }

    private static Task<IResult> GetBorrowingAsync(IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, "/api/settings/borrowing", null, ct);

    private static Task<IResult> GetPenaltyAsync(IHttpClientFactory httpFactory, CancellationToken ct)
        => ProxyForwarder.ForwardAsync(httpFactory, HttpMethod.Get, "/api/settings/penalty", null, ct);
}