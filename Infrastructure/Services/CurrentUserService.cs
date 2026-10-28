namespace LibApp.Infrastructure.Services;

/// <summary>
/// Читает данные текущего пользователя из HttpContext.User.
///
/// На стороне WebAPI HttpContext.User заполняется JwtBearer-middleware
/// из JWT, который подставляет BlazorWeb BFF в AuthenticatedWebApiHandler.
/// JWT формируется JwtService.GenerateToken(...) — claims:
///   · sub                  → library user Id (Guid)
///   · email                → Email
///   · ClaimTypes.Name      → FullName (LastName + FirstName)
///   · ClaimTypes.Role      → UserRole (Reader / Librarian / Admin)
///
/// Никаких запросов в БД не делает — только чтение claims.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Id? LibraryUserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null) return null;

            // В JWT пишется sub, но стандартный inbound-маппинг .NET
            // превращает его в ClaimTypes.NameIdentifier. Пробуем оба.
            var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(sub, out var id) ? new Id(id) : null;
        }
    }

    public string? FullName =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public string? Email
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null) return null;

            return user.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? user.FindFirstValue(ClaimTypes.Email);
        }
    }

    public UserRole? Role
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(role, ignoreCase: false, out var r) ? r : null;
        }
    }
}