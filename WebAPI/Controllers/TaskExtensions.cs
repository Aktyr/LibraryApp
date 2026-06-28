namespace WebAPI.Controllers;

public static class TaskExtensions
{
    public static async Task<object> CastToObject<T>(Task<T> task)
    {
        var result = await task;
        return result!;
    }
}