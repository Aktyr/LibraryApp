namespace WebAPI.Endpoints.MinimalAPI;

public static class CommandExecutor
{
    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<object>>> _cache = new();

    public static async Task<TResponse> ExecuteAsync<TCommand, TRequest, TResponse>(
        IServiceProvider serviceProvider,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
        where TRequest : class
        where TResponse : class
    {
        var command = serviceProvider.GetRequiredService<TCommand>();
        var executor = GetExecutor<TCommand, TRequest, TResponse>();
        var result = await executor(command, request, cancellationToken);
        return (result as TResponse)!;
    }

    private static Func<object, object, CancellationToken, Task<object>> GetExecutor<TCommand, TRequest, TResponse>()
        where TCommand : ICommand
    {
        var commandType = typeof(TCommand);
        return _cache.GetOrAdd(commandType, static (type, requestType) =>
        {
            var executeMethod = type.GetMethod("Execute")
                ?? throw new InvalidOperationException($"Command {type.Name} has no Execute method");

            var commandParam = Expression.Parameter(typeof(object), "command");
            var requestParam = Expression.Parameter(typeof(object), "request");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var typedCommand = Expression.Convert(commandParam, type);
            var typedRequest = Expression.Convert(requestParam, requestType);

            var call = Expression.Call(typedCommand, executeMethod, typedRequest, ctParam);
            var taskType = executeMethod.ReturnType;
            var taskResultType = taskType.GetGenericArguments()[0];

            var castMethod = typeof(TaskExtensions)
                .GetMethod(nameof(TaskExtensions.CastToObject), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(taskResultType);

            var body = Expression.Call(castMethod, call);

            var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task<object>>>(
                body, commandParam, requestParam, ctParam);
            return lambda.Compile();
        }, typeof(TRequest));
    }
}

public static class TaskExtensions
{
    public static async Task<object> CastToObject<T>(Task<T> task)
    {
        var result = await task;
        return result!;
    }
}