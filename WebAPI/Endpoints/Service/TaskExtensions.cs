namespace WebAPI.Endpoints.Service;

/// <summary>
/// Вспомогательный класс для преобразования Task<T> в Task<object>
/// </summary>
public static class TaskExtensions
{
    public static async Task<object> CastToObject<T>(Task<T> task)
    {
        var result = await task;
        return result!;
    }
}