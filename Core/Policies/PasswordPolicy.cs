namespace LibApp.Core.Policies;

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 100;

    /// <summary>Проверяет пароль и возвращает сообщения для UI.</summary>
    public static List<string> Validate(string? password) =>
        ValidateDetailed(password).Select(e => e.Message).ToList();

    /// <summary>
    /// Проверяет пароль и возвращает структурированные ошибки (код + сообщение).
    /// Валидация останавливается на первой ошибке — семантика else if сохранена.
    /// </summary>
    public static List<PasswordError> ValidateDetailed(string? password)
    {
        var errors = new List<PasswordError>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add(new(PasswordPolicyCode.Required, "Пароль обязателен"));
            return errors;
        }

        if (password.Length < MinLength)
            errors.Add(new(PasswordPolicyCode.TooShort, $"Пароль должен быть не менее {MinLength} символов"));
        else if (password.Length > MaxLength)
            errors.Add(new(PasswordPolicyCode.TooLong, "Пароль слишком длинный"));
        else if (!Regex.IsMatch(password, @"\p{Lu}"))
            errors.Add(new(PasswordPolicyCode.RequiresUpper, "Пароль должен содержать хотя бы одну заглавную букву"));
        else if (!Regex.IsMatch(password, @"\p{Ll}"))
            errors.Add(new(PasswordPolicyCode.RequiresLower, "Пароль должен содержать хотя бы одну строчную букву"));
        else if (!Regex.IsMatch(password, @"[0-9]"))
            errors.Add(new(PasswordPolicyCode.RequiresDigit, "Пароль должен содержать хотя бы одну цифру"));
        else if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]"))
            errors.Add(new(PasswordPolicyCode.RequiresNonAlphanumeric, "Пароль должен содержать хотя бы один спецсимвол"));

        return errors;
    }
}


/// <summary>Код ошибки пароля. Нужен для маппинга в IdentityError.</summary>
public enum PasswordPolicyCode
{
    Required,
    TooShort,
    TooLong,
    RequiresUpper,
    RequiresLower,
    RequiresDigit,
    RequiresNonAlphanumeric
}

/// <summary>Структурированная ошибка пароля: код + сообщение для UI.</summary>
public record PasswordError(PasswordPolicyCode Code, string Message);