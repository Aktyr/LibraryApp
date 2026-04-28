namespace LibApp.Infrastructure.DependencyInjection;

public static partial class DI
{    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        var backgroundServiceTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && t.IsSubclassOf(typeof(BackgroundService)));

        foreach (var serviceType in backgroundServiceTypes)
        {
            services.AddTransient(serviceType);
            services.AddHostedService(serviceType);
        }
        return services;
    }
}
