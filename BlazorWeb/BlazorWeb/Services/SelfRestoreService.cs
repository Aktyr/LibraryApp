namespace BlazorWeb.Services;

public class SelfRestoreService(
    IHttpClientFactory httpFactory,
    UserManager<ApplicationUser> userManager,
    IUnitOfWork unitOfWork,
    DeleteUserCommand deleteUserCommand,
    ILogger<SelfRestoreService> logger) : ISelfRestoreService
{
    public async Task SelfRestoreAsync(string email, string password, CancellationToken ct = default)
    {
        // WebAPI: проверить пароль и снять soft-delete в library.
        // Пароль проверяется здесь, а не через Identity: library.PasswordHash — источник истины,
        // у пользователя уже нет Identity-аккаунта.
        var http = httpFactory.CreateClient(ProxyForwarder.WebApiClientName);
        using var resp = await http.PostAsJsonAsync(
            "/api/auth/restore-account",
            new { Email = email, Password = password },
            ct);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            var detail = ProblemDetailsParser.ParseDetail(body)
                ?? $"HTTP {(int)resp.StatusCode}";

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedException(detail);
            if (resp.StatusCode == HttpStatusCode.BadRequest)
                throw new LibValidationException { ExceptionDetails = [detail] };

            logger.LogWarning("Self-restore: WebAPI returned {Status} for {Email}", (int)resp.StatusCode, email);
            throw new InvalidOperationException(detail);
        }

        // library восстановлен — ищем пользователя, чтобы взять Id для привязки Identity.
        var userRepo = unitOfWork.GetRepository<User>();
        var libraryUser = await userRepo.FirstOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new LibValidationException
            {
                ExceptionDetails = ["Пользователь не найден после восстановления"]
            };

        // Identity-пользователь должен быть удалён (Admin-удаление в DeleteAsync).
        // Если он есть — сценарий не self-restore (например, library-удаление упало), нельзя
        // создавать дубликат. Просим обратиться к администратору.
        var existingIdentity = await userManager.FindByEmailAsync(email);
        if (existingIdentity != null)
        {
            logger.LogError(
                "Self-restore: library user {Id} restored, but Identity user {Email} still exists. " +
                "Manual admin intervention required.",
                libraryUser.Id.Value, email);
            throw new LibValidationException
            {
                ExceptionDetails = ["Не удалось создать Identity-аккаунт. Обратитесь к администратору."]
            };
        }

        // Создаём Identity заново с тем же паролем.
        var appUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LibraryUserId = libraryUser.Id.Value,
            FirstName = libraryUser.FirstName,
            LastName = libraryUser.LastName,
            MiddleName = libraryUser.MiddleName,
            ContactInfo = libraryUser.ContactInfo
        };

        var identityResult = await userManager.CreateAsync(appUser, password);
        if (identityResult.Succeeded) return;

        // Компенсация: library восстановлен, но Identity не создан.
        // Откатываем library обратно в soft-delete, чтобы пользователь не остался
        // «восстановленным без возможности входа». DeletedByUserId = null (legacy),
        // повторный self-restore потребует администратора — это осознанный компромисс.
        try
        {
            await deleteUserCommand.Execute(
                new DeleteUserRequest { Id = libraryUser.Id }, ct);
            logger.LogWarning(
                "Self-restore compensation: library user {Id} rolled back to deleted",
                libraryUser.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Self-restore compensation FAILED: library user {Id} is active, Identity not created",
                libraryUser.Id.Value);
        }

        throw new LibValidationException
        {
            ExceptionDetails = identityResult.Errors.Select(e => e.Description).ToList()
        };
    }
}