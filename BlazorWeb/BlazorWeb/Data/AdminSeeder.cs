namespace BlazorWeb.Data;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var provisioning = sp.GetRequiredService<IUserProvisioningService>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var config = sp.GetRequiredService<IConfiguration>();

        var userRepo = uow.GetRepository<LibApp.Core.Entities.User>();
        var adminCount = await userRepo.CountAsync(u => u.Role == UserRole.Admin);
        if (adminCount > 0)
        {
            logger.LogInformation("Admin user already exists ({Count}). Skipping seed.", adminCount);
            return;
        }

        var email = config["Seed:AdminEmail"] ?? "admin@library.local";
        var password = config["Seed:AdminPassword"] ?? "Admin123!";
        var lastName = config["Seed:AdminLastName"] ?? "Administrator";
        var firstName = config["Seed:AdminFirstName"] ?? "System";

        // Если Identity-пользователь с таким email уже есть — используем его,
        // но library-пользователя создаём через сервис, только если LibraryUserId пуст.
        var appUser = await userManager.FindByEmailAsync(email);
        if (appUser is null)
        {
            try
            {
                appUser = await provisioning.CreateAsync(
                    email, password, lastName, firstName, "", email,
                    UserRole.Admin, emailConfirmed: true);
            }
            catch (ProvisioningException pex)
            {
                logger.LogError("Failed to create seed Admin: {Kind} — {Errors}",
                    pex.Kind, string.Join("; ", pex.Errors));
                return;
            }

            logger.LogWarning("===============================================");
            logger.LogWarning("SEED ADMIN CREATED");
            logger.LogWarning("  Email:    {Email}", email);
            logger.LogWarning("  Password: {Password}", password);
            logger.LogWarning("  >>> CHANGE THIS PASSWORD IN PRODUCTION! <<<");
            logger.LogWarning("===============================================");
        }
        else if (appUser.LibraryUserId is null)
        {
            // Identity есть, но без привязки — досоздаём только library-пользователя.
            try
            {
                var libUser = await scope.ServiceProvider.GetRequiredService<IUserService>()
                    .CreateUserAsync(email, password, lastName, firstName, "", email,
                        UserRole.Admin);

                appUser.LibraryUserId = libUser.Id.Value;
                await userManager.UpdateAsync(appUser);
                logger.LogInformation("Library user linked to existing Identity Admin {Email}", email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to link library user to Identity Admin {Email}", email);
            }
        }
        else
        {
            logger.LogInformation("Library user already linked to Identity Admin {Email}", email);
        }
    }
}