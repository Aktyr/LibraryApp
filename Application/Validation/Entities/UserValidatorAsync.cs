namespace LibApp.Application.Validation.Entities;

public class UserValidatorAsync : IValidator
{
    private readonly EmailValidatorAsync _emailValidator;

    public UserValidatorAsync(EmailValidatorAsync emailValidator)
    {
        _emailValidator = emailValidator;
    }
    public async Task<ValidationResponse> ValidateEmailAsync(string email, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var emailResult = await _emailValidator.ValidateAsync(email, ct);
        if (!emailResult.IsValid)
            errors.AddRange(emailResult.Errors);
        return new ValidationResponse(!errors.Any(), errors);
    }

    public Task<ValidationResponse> ValidatePasswordAsync(string password)
    {
        var errors = new List<string>();

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

        return Task.FromResult(new ValidationResponse(!errors.Any(), errors));
    }

    public Task<ValidationResponse> ValidateNameAndContactAsync(string lastName, string firstName, string middleName, string contactInfo)
    {
        var errors = new List<string>();

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

        return Task.FromResult(new ValidationResponse(!errors.Any(), errors));
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

        var emailResult = await ValidateEmailAsync(email, ct);
        if (!emailResult.IsValid)
            errors.AddRange(emailResult.Errors);

        var passwordResult = await ValidatePasswordAsync(password);
        if (!passwordResult.IsValid)
            errors.AddRange(passwordResult.Errors);

        var nameResult = await ValidateNameAndContactAsync(lastName, firstName, middleName, contactInfo);
        if (!nameResult.IsValid)
            errors.AddRange(nameResult.Errors);

        return new ValidationResponse(!errors.Any(), errors);
    }
}