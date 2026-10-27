namespace BlazorWeb.Data;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var userService = sp.GetRequiredService<IUserService>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var config = sp.GetRequiredService<IConfiguration>();

        // Есть ли уже Admin в library.User?
        var userRepo = uow.GetRepository<LibApp.Core.Entities.User>();
        var adminCount = await userRepo.CountAsync(u => u.Role == UserRole.Admin);
        if (adminCount > 0)
        {
            logger.LogInformation("Admin user already exists ({Count}). Skipping seed.", adminCount);
            return;
        }

        // Читаем конфиг (с дефолтами на случай отсутствия секции)
        var email = config["Seed:AdminEmail"] ?? "admin@library.local";
        var password = config["Seed:AdminPassword"] ?? "Admin123!";
        var lastName = config["Seed:AdminLastName"] ?? "Administrator";
        var firstName = config["Seed:AdminFirstName"] ?? "System";

        // Если Identity-пользователь с таким email уже есть — используем его
        var appUser = await userManager.FindByEmailAsync(email);
        if (appUser is null)
        {
            appUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                MiddleName = "",
                ContactInfo = email
            };

            var identityResult = await userManager.CreateAsync(appUser, password);
            if (!identityResult.Succeeded)
            {
                logger.LogError("Failed to create seed Admin Identity user: {Errors}",
                    string.Join(", ", identityResult.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Identity Admin user created: {Email}", email);
        }

        // Создаём library.User (если ещё не связан)
        if (appUser.LibraryUserId is null)
        {
            try
            {
                var libUser = await userService.CreateUserAsync(
                    email, password,
                    lastName, firstName, "",
                    email,
                    UserRole.Admin);

                appUser.LibraryUserId = libUser.Id.Value;
                await userManager.UpdateAsync(appUser);

                logger.LogWarning("===============================================");
                logger.LogWarning("SEED ADMIN CREATED");
                logger.LogWarning("  Email:    {Email}", email);
                logger.LogWarning("  Password: {Password}", password);
                logger.LogWarning("  >>> CHANGE THIS PASSWORD IN PRODUCTION! <<<");
                logger.LogWarning("===============================================");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create seed Admin library user.");
                // Откатываем Identity, чтобы не оставить «сироту»
                await userManager.DeleteAsync(appUser);
            }
        }
        else
        {
            logger.LogInformation("Library user already linked to Identity Admin {Email}", email);
        }
    }
}