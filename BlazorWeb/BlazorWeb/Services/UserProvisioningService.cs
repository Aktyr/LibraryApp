namespace BlazorWeb.Services;

public class UserProvisioningService(
    UserManager<ApplicationUser> userManager,
    IUserService userService,
    ILogger<UserProvisioningService> logger) : IUserProvisioningService
{
    public async Task<ApplicationUser> CreateAsync(
        string email,
        string password,
        string lastName,
        string firstName,
        string middleName,
        string contactInfo,
        UserRole role,
        bool emailConfirmed = true,
        CancellationToken ct = default)
    {
        var appUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            ContactInfo = contactInfo
        };

        var identityResult = await userManager.CreateAsync(appUser, password);
        if (!identityResult.Succeeded)
        {
            throw new ProvisioningException(
                ProvisioningErrorKind.IdentityError,
                identityResult.Errors.Select(e => e.Description).ToList());
        }

        try
        {
            var libUser = await userService.CreateUserAsync(
                email, password, lastName, firstName, middleName, contactInfo,
                role, ct);

            appUser.LibraryUserId = libUser.Id.Value;

            var updateResult = await userManager.UpdateAsync(appUser);
            if (!updateResult.Succeeded)
            {
                throw new ProvisioningException(
                    ProvisioningErrorKind.IdentityError,
                    updateResult.Errors.Select(e => e.Description).ToList());
            }

            return appUser;
        }
        catch (LibValidationException lve)
        {
            await SafeDeleteAsync(appUser);
            throw new ProvisioningException(
                ProvisioningErrorKind.LibraryError, lve.ExceptionDetails, lve);
        }
        catch (ProvisioningException)
        {
            await SafeDeleteAsync(appUser);
            throw;
        }
        catch (Exception ex)
        {
            await SafeDeleteAsync(appUser);
            logger.LogError(ex, "Provisioning failed for {Email}", email);
            throw new ProvisioningException(
                ProvisioningErrorKind.Unknown,
                ["Не удалось завершить создание пользователя, попробуйте позже"],
                ex);
        }
    }

    private async Task SafeDeleteAsync(ApplicationUser appUser)
    {
        try { await userManager.DeleteAsync(appUser); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rollback Identity user {Email}", appUser.Email);
        }
    }
}