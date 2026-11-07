namespace BlazorWeb.Services;

public sealed class BffAuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BffAuditService> _logger;

    public BffAuditService(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        ILogger<BffAuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task LogAsync(
        AuditActionType action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        Id? userId = null,
        string? userName = null,
        CancellationToken ct = default)
    {
        var finalUserId = userId;
        var finalUserName = userName;

        var httpUser = _httpContextAccessor.HttpContext?.User;

        // Если команда не передала userId явно — пробуем достать library-Id
        // из текущего Identity-пользователя.
        // Если команда не передала userId явно — пробуем достать library-Id
        // из текущего Identity-пользователя.
        if (finalUserId is null && httpUser?.Identity?.IsAuthenticated == true)
        {
            var identityUserId = httpUser.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(identityUserId))
            {
                try
                {
                    var appUser = await _userManager.FindByIdAsync(identityUserId);
                    if (appUser?.LibraryUserId is Guid libraryUserId)
                    {
                        finalUserId = new Id(libraryUserId);

                        // Если userName не передан явно — достаём FullName из library.User.
                        // ClaimTypes.Name в BFF содержит email (UserName = Email), это не подпись.
                        if (string.IsNullOrWhiteSpace(finalUserName))
                        {
                            var libUser = await _unitOfWork.GetRepository<LibApp.Core.Entities.User>()
                                .FirstOrDefaultAsync(u => u.Id == finalUserId, ct);
                            if (libUser != null)
                            {
                                finalUserName = libUser.FullName;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось определить LibraryUserId для аудита BFF");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(finalUserName))
        {
            finalUserName = "Неизвестный пользователь";
        }

        var log = new AuditLog
        {
            Id = new Id(Guid.NewGuid()),
            Timestamp = DateTime.UtcNow,
            UserId = finalUserId,
            UserName = finalUserName,
            ActionType = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        };

        var repo = _unitOfWork.GetRepository<AuditLog>();
        await repo.AddAsync(log, ct);

        // ВАЖНО: SaveChangesAsync делает вызывающая команда.
        // Так запись аудита попадает в ту же транзакцию, что и основное действие.
    }
}