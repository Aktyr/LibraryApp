namespace BlazorWeb.Client;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();


        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7271/";
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
        builder.Services.AddScoped<IBookApiClient, BookApiClient>();


        await builder.Build().RunAsync();
    }
}