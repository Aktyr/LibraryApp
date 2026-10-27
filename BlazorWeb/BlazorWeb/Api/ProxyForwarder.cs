namespace BlazorWeb.Api;

/// <summary>
/// Общий HTTP-форвардер для прокси-эндпоинтов BFF.
/// Пересылает запрос в WebAPI и возвращает ответ как есть: тело, content-type, status code.
///
/// JWT подставляет <see cref="AuthenticatedWebApiHandler"/> в цепочке HttpClient "webapi",
/// поэтому здесь никакой авторизации не требуется.
/// </summary>
public static class ProxyForwarder
{
    public const string WebApiClientName = "webapi";

    public static async Task<IResult> ForwardAsync(
        IHttpClientFactory httpFactory,
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct)
    {
        var http = httpFactory.CreateClient(WebApiClientName);
        using var req = new HttpRequestMessage(method, path);
        if (body is not null)
            req.Content = JsonContent.Create(body);

        using var resp = await http.SendAsync(req, ct);
        var content = await resp.Content.ReadAsStringAsync(ct);
        var mediaType = resp.Content.Headers.ContentType?.MediaType ?? "application/json";
        return Results.Content(content, mediaType, statusCode: (int)resp.StatusCode);
    }
}