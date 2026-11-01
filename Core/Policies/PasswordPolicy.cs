namespace LibApp.Core.Policies;

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 100;

    public static List<string> Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Пароль обязателен");
            return errors;
        }

        if (password.Length < MinLength)
            errors.Add($"Пароль должен быть не менее {MinLength} символов");
        else if (password.Length > MaxLength)
            errors.Add("Пароль слишком длинный");
        else if (!Regex.IsMatch(password, @"\p{Lu}"))
            errors.Add("Пароль должен содержать хотя бы одну заглавную букву");
        else if (!Regex.IsMatch(password, @"\p{Ll}"))
            errors.Add("Пароль должен содержать хотя бы одну строчную букву");
        else if (!Regex.IsMatch(password, @"[0-9]"))
            errors.Add("Пароль должен содержать хотя бы одну цифру");
        else if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]"))
            errors.Add("Пароль должен содержать хотя бы один спецсимвол");

        return errors;
    }
}