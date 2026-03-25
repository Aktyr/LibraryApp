using LibApp.Infrastructure.DependencyInjection;
using LibApp.Infrastructure.Db.EFCore.Postgre;
using Microsoft.EntityFrameworkCore;

namespace WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Регистрация сервисов через нашу DI
        builder.Services
            .AddDatabase(builder.Configuration)
            .AddRepositories()
            .AddConverters()
            .AddValidators()
            .AddCommands()
            .AddJwtAuthentication(builder.Configuration);

        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddBackgroundServices();

        var app = builder.Build();

        // Применяем миграции при запуске
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LibraryContext>();
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
        app.MapControllers();
        app.Run();

    }
}
