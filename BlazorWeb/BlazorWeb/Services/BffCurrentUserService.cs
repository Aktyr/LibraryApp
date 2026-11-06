namespace BlazorWeb.Services;

/// <summary>
/// ICurrentUserService для BFF. В BFF HttpContext.User — это Identity-пользователь,
/// его NameIdentifier — Identity-id, а не library-id. Резолвим library-данные через
/// UserManager + библиотечный репозиторий (тот же паттерн, что в BffAuditService).
///
/// Sync-over-async (GetAwaiter().GetResult()): интерфейс ICurrentUserService синхронный,
/// а UserManager и репозиторий — асинхронные. В BFF это допустимо: scoped-lifetime,
/// резолв один раз за запрос (кеш _resolved), запрос короткий.
/// </summary>
public class BffCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    private bool _resolved;
    private Id? _libraryUserId;
    private string? _fullName;
    private string? _email;
    private UserRole? _role;

    public BffCurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public Id? LibraryUserId { get { Resolve(); return _libraryUserId; } }
    public string? FullName { get { Resolve(); return _fullName; } }
    public string? Email { get { Resolve(); return _email; } }
    public UserRole? Role { get { Resolve(); return _role; } }

    private void Resolve()
    {
        if (_resolved) return;
        _resolved = true;

        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated != true) return;

        var identityId = httpUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(identityId)) return;

        var appUser = _userManager.FindByIdAsync(identityId).GetAwaiter().GetResult();
        if (appUser?.LibraryUserId is not Guid libraryUserId) return;

        var libId = new Id(libraryUserId);
        var libUser = _unitOfWork.GetRepository<LibApp.Core.Entities.User>()
            .FirstOrDefaultAsync(u => u.Id == libId)
            .GetAwaiter().GetResult();

        if (libUser is null) return;

        _libraryUserId = libUser.Id;
        _fullName = libUser.FullName;
        _email = libUser.Email;
        _role = libUser.Role;
    }
}