namespace WebAPI.Misc.Helpers;

public static class LibTaskExtensions
{
    public static async Task<object> CastToObject<T>(Task<T> task)
    {
        var result = await task;
        return result!;
    }
}