namespace LibApp.Application.Helpers;

public static class ResponseFactory
{
    private static readonly Dictionary<Type, string> EntityDisplayNames = new()
    {
        { typeof(UserRoomBook), "Borrowing record" },
        { typeof(RoomBook), "Book copy" },
        { typeof(DiscardedBook), "Discarded book" },
    };

    #region CRUD Methods

    public static BasicCreateDeleteResponse Created<T>() =>
        new("Ok", $"{GetEntityName<T>()} is created.");

    public static BasicCreateDeleteResponse Updated<T>() =>
        new("Ok", $"{GetEntityName<T>()} updated successfully.");

    public static BasicCreateDeleteResponse Deleted<T>() =>
        new("Ok", $"{GetEntityName<T>()} deleted successfully.");

    public static BasicCreateDeleteResponse Success(string message) =>
        new("Ok", message);

    public static BasicCreateDeleteResponse Error(string message) =>
        new("Error", message);

    #endregion

    #region Get/List Methods

    public static TResponse List<TEntity, TDto, TResponse>(TDto[] data)
        where TResponse : IGetResponse
    {
        var entityName = GetEntityName<TEntity>();
        var count = data?.Length ?? 0;
        var message = $"List of {entityName}s retrieved successfully. Total: {count}.";

        return CreateResponse<TResponse, TDto>(message, data ?? Array.Empty<TDto>());
    }

    public static TResponse Single<TEntity, TDto, TResponse>(TDto data)
        where TResponse : IGetResponse
    {
        var entityName = GetEntityName<TEntity>();
        var message = $"{entityName} retrieved successfully.";

        return CreateResponse<TResponse, TDto>(message, new[] { data });
    }

    public static TResponse Found<TEntity, TDto, TResponse>(TDto[] data)
        where TResponse : IGetResponse
    {
        var entityName = GetEntityName<TEntity>();
        var count = data?.Length ?? 0;
        var message = $"Found {count} {entityName.ToLower()}{(count != 1 ? "s" : "")}.";

        return CreateResponse<TResponse, TDto>(message, data ?? Array.Empty<TDto>());
    }

    public static TResponse Empty<TEntity, TDto, TResponse>()
        where TResponse : IGetResponse
    {
        var entityName = GetEntityName<TEntity>();
        var message = $"No {entityName.ToLower()}s found.";

        return CreateResponse<TResponse, TDto>(message, Array.Empty<TDto>());
    }

    #endregion

    #region Helper Methods

    private static TResponse CreateResponse<TResponse, TDto>(string message, TDto[] data)
        where TResponse : IGetResponse
    {
        // Ищем конструктор с параметрами (string, string, TDto[])
        var ctor = typeof(TResponse).GetConstructor(new[] { typeof(string), typeof(string), typeof(TDto[]) });
        if (ctor != null)
        {
            return (TResponse)ctor.Invoke(new object[] { "Ok", message, data });
        }

        throw new InvalidOperationException(
            $"Cannot create response of type {typeof(TResponse).Name}. " +
            $"Expected constructor: (string Status, string Message, {typeof(TDto).Name}[] Data)");
    }

    private static string GetEntityName<T>()
    {
        var type = typeof(T);
        return EntityDisplayNames.TryGetValue(type, out var name) ? name : type.Name;
    }

    #endregion
}