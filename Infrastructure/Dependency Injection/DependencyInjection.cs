namespace LibApp.Infrastructure.DependencyInjection;

// Пока не применяется 
public static class DependencyInjection
{
    //public static IServiceCollection AddRepositories(this IServiceCollection services)
    //{
    //    services.AddSingleton(typeof(LibraryDataContext<>));
    //    services.AddScoped(typeof(IRepository<>), typeof(CachedRepository<>));
    //    return services;
    //}

    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
         services.AddScoped<Book>();
         services.AddScoped<User>();
         services.AddScoped<Room>();
         services.AddScoped<RoomBook>();
         services.AddScoped<UserRoomBook>();

        return services;
    }
    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddScoped<BookValidatorAsync>();
        services.AddScoped<RoomValidatorAsync>();
        services.AddScoped<RoomBookValidatorAsync>();
        services.AddScoped<UserValidatorAsync>();
        //services.AddScoped<UserRoomBookValidatorAsync>();

        return services;
    }
}
