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

            // WebAPI вернул 400 «Аккаунт не деактивирован» — library уже восстановлен
            // прямым вызовом /api/auth/restore-account или предыдущей неудачной попыткой.
            // Продолжаем выполнение, чтобы довести до конца проверку Identity.
            if (resp.StatusCode == HttpStatusCode.BadRequest
                && detail.Contains("не деактивирован", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation(
                    "Self-restore: library already restored for {Email}, continuing with Identity check", email);
            }
            else if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new LibValidationException { ExceptionDetails = [detail] };
            }
            else
            {
                logger.LogWarning("Self-restore: WebAPI returned {Status} for {Email}", (int)resp.StatusCode, email);
                throw new InvalidOperationException(detail);
            }
        }

        // library восстановлен — ищем пользователя, чтобы взять Id для привязки Identity.
        var userRepo = unitOfWork.GetRepository<User>();
        var libraryUser = await userRepo.FirstOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new LibValidationException
            {
                ExceptionDetails = ["Пользователь не найден после восстановления"]
            };

        var existingIdentity = await userManager.FindByEmailAsync(email);
        if (existingIdentity != null)
        {
            // Если Identity уже связан с восстановленным library-пользователем, значит WebAPI восстановил
            // запись в обход BFF или предыдущий вызов упал после восстановления library. Создавать
            // Identity заново нельзя — рабочий аккаунт уже существует, достаточно пропустить создание.
            if (existingIdentity.LibraryUserId == libraryUser.Id.Value)
            {
                logger.LogInformation(
                    "Self-restore: library user {Id} restored, Identity {Email} already linked. Ready to log in.",
                    libraryUser.Id.Value, email);
                return;
            }

            // LibraryUserId не совпадает: email совпадает, но это другой пользователь.
            logger.LogError(
                "Self-restore: library user {Id} restored, but Identity {Email} linked to different library user {OtherId}",
                libraryUser.Id.Value, email, existingIdentity.LibraryUserId);
            throw new LibValidationException
            {
                ExceptionDetails = ["Конфликт данных аккаунта. Обратитесь к администратору."]
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