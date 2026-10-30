namespace WebAPI.Endpoints.BaseAPI;

[ApiController]
[Obsolete("This class is deprecated. Use the HybridAPI.")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IServiceProvider _serviceProvider;

    protected BaseApiController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected Task<TResponse> ExecuteCommand<TCommand, TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
        where TRequest : class
        where TResponse : class
        => CommandExecutor.ExecuteAsync<TCommand, TRequest, TResponse>(
            _serviceProvider, request, cancellationToken);

    protected Task<TResponse> ExecuteQuery<TQuery, TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TQuery : ICommand
        where TRequest : class
        where TResponse : class
        => CommandExecutor.ExecuteAsync<TQuery, TRequest, TResponse>(
            _serviceProvider, request, cancellationToken);
}