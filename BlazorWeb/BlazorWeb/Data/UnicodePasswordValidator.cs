namespace BlazorWeb.Data;

/// <summary>
/// Замена стандартного Identity PasswordValidator. Правила — из <see cref="PasswordPolicy"/> (SSOT),
/// здесь только маппинг в <see cref="IdentityError"/>.
///
/// Стандартный Microsoft.AspNetCore.Identity.PasswordValidator на .NET 10 ложно отклоняет
/// кириллические пароли ("Пароль123!"): использует char.IsLower/IsUpper, которые для не-ASCII
/// работают не так, как ожидается. <see cref="PasswordPolicy"/> использует regex \p{Ll}/\p{Lu},
/// что корректно для любого алфавита.
///
/// PasswordPolicyCode.TooLong НЕ маппится в IdentityError: Identity не поддерживает
/// максимальную длину пароля. Правило MaxLength живёт в PasswordPolicy и проверяется
/// отдельно в UserService.CreateUserAsync.
/// </summary>
public class UnicodePasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        var errors = PasswordPolicy.ValidateDetailed(password)
            .Where(e => e.Code is not PasswordPolicyCode.TooLong)
            .ToList();

        if (errors.Count == 0)
            return Task.FromResult(IdentityResult.Success);

        return Task.FromResult(IdentityResult.Failed(
            errors.Select(MapToIdentityError).ToArray()));
    }

    private static IdentityError MapToIdentityError(PasswordError error) => new()
    {
        Code = ToIdentityCode(error.Code),
        Description = error.Message
    };

    /// <summary>
    /// Коды для IdentityError.Code — через nameof, чтобы не разъезжаться с IdentityErrorDescriber.
    /// Своего PasswordTooLong здесь нет — это правило Identity не поддерживает.
    /// </summary>
    private static class IdentityPasswordCodes
    {
        public static readonly string TooShort = nameof(IdentityErrorDescriber.PasswordTooShort);
        public static readonly string RequiresUpper = nameof(IdentityErrorDescriber.PasswordRequiresUpper);
        public static readonly string RequiresLower = nameof(IdentityErrorDescriber.PasswordRequiresLower);
        public static readonly string RequiresDigit = nameof(IdentityErrorDescriber.PasswordRequiresDigit);
        public static readonly string RequiresNonAlphanumeric = nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric);
    }

    private static string ToIdentityCode(PasswordPolicyCode code) => code switch
    {
        PasswordPolicyCode.Required or PasswordPolicyCode.TooShort => IdentityPasswordCodes.TooShort,
        PasswordPolicyCode.RequiresUpper => IdentityPasswordCodes.RequiresUpper,
        PasswordPolicyCode.RequiresLower => IdentityPasswordCodes.RequiresLower,
        PasswordPolicyCode.RequiresDigit => IdentityPasswordCodes.RequiresDigit,
        PasswordPolicyCode.RequiresNonAlphanumeric => IdentityPasswordCodes.RequiresNonAlphanumeric,
        PasswordPolicyCode.TooLong => throw new InvalidOperationException(
            "PasswordPolicyCode.TooLong должен быть отфильтрован до маппинга в Identity: " +
            "Identity не поддерживает максимальную длину пароля."),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown password policy code")
    };
}