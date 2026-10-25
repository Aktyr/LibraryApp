namespace BlazorWeb.Api;


/// <summary>
/// DelegatingHandler, который подставляет JWT в каждый исходящий запрос к WebAPI.
/// JWT выпускается из данных текущего Identity-пользователя + Library-пользователя.
/// Role берётся из library.User (единственный источник истины), не из Identity.
/// </summary>
public class AuthenticatedWebApiHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthenticatedWebApiHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("Proxy request without authenticated user: {Path}", request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogError("Authenticated user has no NameIdentifier claim");
            return await base.SendAsync(request, cancellationToken);
        }

        // ВАЖНО: IHttpClientFactory кэширует цепочку handler'ов между запросами.
        // Если инжектить scoped-сервисы в конструктор, они захватят DbContext
        // первого запроса и будут конфликтовать при параллельных вызовах.
        // Поэтому резолвим из RequestServices — там правильный scope текущего запроса.
        var scopedServices = httpContext.RequestServices;
        var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
        var unitOfWork = scopedServices.GetRequiredService<IUnitOfWork>();
        var jwtService = scopedServices.GetRequiredService<JwtService>();

        var appUser = await userManager.FindByIdAsync(userId);
        if (appUser is null)
        {
            logger.LogError("ApplicationUser {UserId} not found", userId);
            return await base.SendAsync(request, cancellationToken);
        }

        if (appUser.LibraryUserId is null)
        {
            logger.LogError("ApplicationUser {UserId} has no LibraryUserId", userId);
            return await base.SendAsync(request, cancellationToken);
        }

        var libraryUserId = appUser.LibraryUserId.Value;
        var id = new Id(libraryUserId);
        var libraryUser = await unitOfWork.GetRepository<LibApp.Core.Entities.User>()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (libraryUser is null)
        {
            logger.LogError("Library user {LibraryUserId} not found", libraryUserId);
            return await base.SendAsync(request, cancellationToken);
        }

        var token = jwtService.GenerateToken(
            libraryUser.Id.Value,
            libraryUser.Email,
            libraryUser.FullName,
            libraryUser.Role);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}