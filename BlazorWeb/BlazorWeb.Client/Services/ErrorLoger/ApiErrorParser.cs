using System.Text.Json;

namespace BlazorWeb.Client.Services;

internal static class ApiErrorParser
{
    public static async Task<ApiException> ParseAsync(
        HttpResponseMessage resp, CancellationToken ct)
    {
        var status = (int)resp.StatusCode;
        var text = await resp.Content.ReadAsStringAsync(ct);

        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("detail", out var detail)
                        && detail.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(detail.GetString()))
                    {
                        return new ApiException(status, detail.GetString()!);
                    }
                    if (doc.RootElement.TryGetProperty("title", out var title)
                        && title.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(title.GetString()))
                    {
                        return new ApiException(status, title.GetString()!);
                    }
                }
            }
            catch (JsonException)
            {
                // ответ не JSON — падаем на сырой текст ниже
            }
        }

        return new ApiException(status, $"HTTP {status} {resp.ReasonPhrase}");
    }
}