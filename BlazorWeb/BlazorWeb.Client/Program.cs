namespace BlazorWeb.Client;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

        #region API-клиенты для компонентов Blazor.WebAssembly (через прокси-эндпоинты)
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
        });
        builder.Services.AddScoped<IBookApiClient, BookApiClient>();
        builder.Services.AddScoped<IRoomApiClient, RoomApiClient>();
        builder.Services.AddScoped<IUserApiClient, UserApiClient>();
        builder.Services.AddScoped<IBorrowingApiClient, BorrowingApiClient>();
        #endregion

        await builder.Build().RunAsync();
    }
}