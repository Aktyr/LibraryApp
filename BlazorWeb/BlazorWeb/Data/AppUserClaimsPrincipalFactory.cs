namespace BlazorWeb.Data;

public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppUserClaimsPrincipalFactory> _logger;

    public AppUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> options,
        IUnitOfWork unitOfWork,
        ILogger<AppUserClaimsPrincipalFactory> logger)
        : base(userManager, options)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);

        try
        {
            if (user.LibraryUserId.HasValue)
            {
                var id = new Id(user.LibraryUserId.Value);
                var libUser = await _unitOfWork.GetRepository<LibApp.Core.Entities.User>()
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (libUser is not null)
                {
                    var identity = (ClaimsIdentity)principal.Identity!;
                    identity.AddClaim(new Claim(ClaimTypes.Role, libUser.Role.ToString()));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to load library role for user {UserId}. Falling back to no role.",
                user.Id);
        }

        return principal;
    }
}