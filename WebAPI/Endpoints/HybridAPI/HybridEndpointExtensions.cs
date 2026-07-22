namespace WebAPI.Endpoints.HybridAPI;

public static class HybridEndpointExtensions
{
    public static IEndpointRouteBuilder MapHybridEndpoints(this IEndpointRouteBuilder app)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var endpointTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(EndpointBase)));

        foreach (var type in endpointTypes)
        {
            // Создаём экземпляр только для вызова метода MapEndpoint.
            // Можно было бы сделать MapEndpoint статическим и передавать тип, но оставим как есть.
            var instance = (EndpointBase)ActivatorUtilities.CreateInstance(app.ServiceProvider, type);
            instance.MapEndpoint(app);
        }

        return app;
    }
}