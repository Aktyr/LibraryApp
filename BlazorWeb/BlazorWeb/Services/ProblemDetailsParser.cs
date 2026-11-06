namespace BlazorWeb.Services;

/// <summary>Извлекает поле detail из ProblemDetails-ответа WebAPI. Возвращает null, если не удалось.</summary>
internal static class ProblemDetailsParser
{
    public static string? ParseDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            if (doc.RootElement.TryGetProperty("detail", out var detail)
                && detail.ValueKind == JsonValueKind.String)
            {
                var value = detail.GetString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}