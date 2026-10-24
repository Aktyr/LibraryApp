namespace LibApp.Infrastructure.DependencyInjection;

public static partial class DI
{
    public static IServiceCollection AddConfigurationSettings(this IServiceCollection services)
    {
        // Регистрируем все настройки с валидацией при старте
        var settingsTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && typeof(ISettings).IsAssignableFrom(t));

        foreach (var type in settingsTypes)
        {
            typeof(DI).GetMethod(nameof(ConfigureAndValidate), BindingFlags.NonPublic | BindingFlags.Static)!
                                       .MakeGenericMethod(type)
                                       .Invoke(null, [services]);
        }
        return services;
    }

    private static IServiceCollection ConfigureAndValidate<T>(this IServiceCollection services) where T : class
    {
        // Вырезаем "Settings" для лучшего нейминга
        var sectionName = typeof(T).Name.Replace("Settings", "");

        // Все секции под "ApplicationSettings:*" в appsettings.json
        return services.AddOptions<T>()
                       .BindConfiguration($"ApplicationSettings:{sectionName}")
                       .ValidateDataAnnotations()
                       .ValidateOnStart()
                       .Services;
    }
}