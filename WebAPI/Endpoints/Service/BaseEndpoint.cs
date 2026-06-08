namespace WebAPI.Endpoints.Service;

[ApiController]
public abstract class BaseEndpoint : ControllerBase
{
    protected readonly IServiceProvider _serviceProvider;

    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<object>>> _executeDelegateCache = new();

    protected BaseEndpoint(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Выполняет команду (CREATE, UPDATE, DELETE)
    /// </summary>
    protected async Task<TResponse> ExecuteCommand<TCommand, TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
        where TRequest : class
        where TResponse : class
    {
        var command = _serviceProvider.GetRequiredService<TCommand>();
        var executor = GetExecutor<TCommand, TRequest, TResponse>();
        var result = await executor(command, request, cancellationToken);
        return (result as TResponse)!;
    }

    /// <summary>
    /// Выполняет запрос (GET)
    /// </summary>
    protected async Task<TResponse> ExecuteQuery<TQuery, TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TQuery : ICommand
        where TRequest : class
        where TResponse : class
    {
        var query = _serviceProvider.GetRequiredService<TQuery>();
        var executor = GetExecutor<TQuery, TRequest, TResponse>();
        var result = await executor(query, request, cancellationToken);
        return (result as TResponse)!;
    }

    private static Func<object, object, CancellationToken, Task<object>> GetExecutor<TCommand, TRequest, TResponse>()
        where TCommand : ICommand
    {
        var commandType = typeof(TCommand);

        return _executeDelegateCache.GetOrAdd(commandType, static (type, requestType) =>
        {
            var executeMethod = type.GetMethod("Execute")
                ?? throw new InvalidOperationException($"Command {type.Name} doesn't have Execute method");

            // Параметры
            var commandParam = Expression.Parameter(typeof(object), "command");
            var requestParam = Expression.Parameter(typeof(object), "request");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            // Приведение типов
            var typedCommand = Expression.Convert(commandParam, type);
            var typedRequest = Expression.Convert(requestParam, requestType);

            // Вызов метода Execute
            var call = Expression.Call(typedCommand, executeMethod, typedRequest, ctParam);

            // Получаем Task<T>
            var taskType = executeMethod.ReturnType;
            var taskResultType = taskType.GetGenericArguments()[0];

            // Создаем метод CastToObject<T>
            var castMethod = typeof(TaskExtensions)
                .GetMethod(nameof(TaskExtensions.CastToObject), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(taskResultType);

            // Вызываем CastToObject
            var body = Expression.Call(castMethod, call);

            // Создаем лямбду
            var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task<object>>>(
                body, commandParam, requestParam, ctParam);

            return lambda.Compile();
        }, typeof(TRequest));
    }
}
