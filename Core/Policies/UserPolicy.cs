namespace LibApp.Core.Policies;

public static class UserPolicy
{
    public const int MaxNameLength = 100;
    public const int MaxContactInfoLength = 200;

    public static List<string> ValidateNameAndContact(
        string? lastName,
        string? firstName,
        string? middleName,
        string? contactInfo)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(lastName))
            errors.Add("Фамилия обязательна");
        else if (lastName.Length > MaxNameLength)
            errors.Add($"Фамилия не может превышать {MaxNameLength} символов");

        if (string.IsNullOrWhiteSpace(firstName))
            errors.Add("Имя обязательно");
        else if (firstName.Length > MaxNameLength)
            errors.Add($"Имя не может превышать {MaxNameLength} символов");

        if (middleName?.Length > MaxNameLength)
            errors.Add($"Отчество не может превышать {MaxNameLength} символов");

        if (string.IsNullOrWhiteSpace(contactInfo))
            errors.Add("Контактная информация обязательна");
        else if (contactInfo.Length > MaxContactInfoLength)
            errors.Add($"Контактная информация не может превышать {MaxContactInfoLength} символов");

        return errors;
    }
}