namespace WebAPI.Endpoints.MinimalAPI;

public static class MinimalApiController
{
    public static Task<TResponse> ExecuteAsync<TCommand, TRequest, TResponse>(
        IServiceProvider serviceProvider,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
        where TRequest : class
        where TResponse : class
        => CommandExecutor.ExecuteAsync<TCommand, TRequest, TResponse>(
            serviceProvider, request, cancellationToken);
}