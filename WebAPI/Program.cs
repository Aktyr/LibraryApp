namespace WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Регистрация сервисов через DI
        builder.Services.AddAllServices();

        builder.Services.AddAuthorization();
        //builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();

        var app = builder.Build();

        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        // Применяем миграции при запуске
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LibraryContext>();
            if (app.Environment.IsDevelopment())
                dbContext.Database.Migrate();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        //app.MapControllers();
        //app.MapEndpoints();
        app.MapHybridEndpoints();

        app.Run();
    }
}