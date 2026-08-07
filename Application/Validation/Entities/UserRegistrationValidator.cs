namespace LibApp.Application.Validation.Entities;

public class UserRegistrationValidator : IValidator
{
    private readonly EmailValidatorAsync _emailValidator;

    public UserRegistrationValidator(EmailValidatorAsync emailValidator)
    {
        _emailValidator = emailValidator;
    }

    public async Task<ValidationResponse> ValidateAllAsync(
        string email,
        string password,
        string lastName,
        string firstName,
        string middleName,
        string contactInfo,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        // Email
        var emailResult = await _emailValidator.ValidateAsync(email, ct);
        if (!emailResult.IsValid)
            errors.AddRange(emailResult.Errors);

        // Пароль
        if (string.IsNullOrWhiteSpace(password))
            errors.Add("Пароль обязателен");
        else if (password.Length < 8)
            errors.Add("Пароль должен быть не менее 8 символов");
        else if (password.Length > 100)
            errors.Add("Пароль слишком длинный");
        else if (!Regex.IsMatch(password, @"[A-Z]"))
            errors.Add("Пароль должен содержать хотя бы одну заглавную букву");
        else if (!Regex.IsMatch(password, @"[a-z]"))
            errors.Add("Пароль должен содержать хотя бы одну строчную букву");
        else if (!Regex.IsMatch(password, @"[0-9]"))
            errors.Add("Пароль должен содержать хотя бы одну цифру");
        else if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]"))
            errors.Add("Пароль должен содержать хотя бы один спецсимвол");

        // ФИО и контакты
        if (string.IsNullOrWhiteSpace(lastName))
            errors.Add("Фамилия обязательна");
        else if (lastName.Length > 100)
            errors.Add("Фамилия не может превышать 100 символов");

        if (string.IsNullOrWhiteSpace(firstName))
            errors.Add("Имя обязательно");
        else if (firstName.Length > 100)
            errors.Add("Имя не может превышать 100 символов");

        if (middleName?.Length > 100)
            errors.Add("Отчество не может превышать 100 символов");

        if (string.IsNullOrWhiteSpace(contactInfo))
            errors.Add("Контактная информация обязательна");
        else if (contactInfo.Length > 200)
            errors.Add("Контактная информация не может превышать 200 символов");

        await Task.CompletedTask;
        return new ValidationResponse(!errors.Any(), errors);
    }
}