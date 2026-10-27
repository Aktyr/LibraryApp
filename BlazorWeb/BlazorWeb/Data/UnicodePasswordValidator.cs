namespace BlazorWeb.Data;

/// <summary>
/// Замена стандартного Identity PasswordValidator, поддерживающая Unicode (кириллицу).
///
/// Стандартный Microsoft.AspNetCore.Identity.PasswordValidator полагается на
/// char.IsLower/char.IsUpper, которые должны работать с кириллицей по документации,
/// но на практике на .NET 10 для паролей типа "Пароль123!" ложно ругаются
/// "Passwords must have at least one lowercase ('a'-'z')".
///
/// Этот валидатор использует regex-категории \p{Ll}/\p{Lu}, что совпадает
/// с логикой UserValidatorAsync (library) и работает для любого алфавита.
///
/// Коды ошибок (Code) совпадают со стандартными IdentityErrorDescriber.*,
/// чтобы клиент мог обрабатывать их единообразно.
/// </summary>
public class UnicodePasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        var opts = manager.Options.Password;
        var errors = new List<IdentityError>();

        if (string.IsNullOrWhiteSpace(password))
        {
            if (opts.RequiredLength > 0)
            {
                errors.Add(new IdentityError
                {
                    Code = nameof(IdentityErrorDescriber.PasswordTooShort),
                    Description = $"Пароль должен быть не менее {opts.RequiredLength} символов"
                });
            }
            return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
        }

        if (password.Length < opts.RequiredLength)
            errors.Add(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordTooShort),
                Description = $"Пароль должен быть не менее {opts.RequiredLength} символов"
            });

        if (opts.RequireLowercase && !Regex.IsMatch(password, @"\p{Ll}"))
            errors.Add(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordRequiresLower),
                Description = "Пароль должен содержать хотя бы одну строчную букву"
            });

        if (opts.RequireUppercase && !Regex.IsMatch(password, @"\p{Lu}"))
            errors.Add(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordRequiresUpper),
                Description = "Пароль должен содержать хотя бы одну заглавную букву"
            });

        if (opts.RequireDigit && !password.Any(char.IsDigit))
            errors.Add(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordRequiresDigit),
                Description = "Пароль должен содержать хотя бы одну цифру"
            });

        if (opts.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            errors.Add(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
                Description = "Пароль должен содержать хотя бы один спецсимвол"
            });

        if (opts.RequiredUniqueChars > 1 && password.Distinct().Count() < opts.RequiredUniqueChars)
            errors.Add(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars),
                Description = $"Пароль должен содержать минимум {opts.RequiredUniqueChars} уникальных символов"
            });

        return Task.FromResult(errors.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(errors.ToArray()));
    }
}