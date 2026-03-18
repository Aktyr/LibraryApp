namespace LibApp.Application.Validation.Auth;

public class RegisterValidatorAsync
{
    public async Task<ValidationResult> ValidateAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("Email обязателен");
        else if (!IsValidEmail(request.Email))
            errors.Add("Некорректный формат email");

        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Пароль обязателен");
        else if (request.Password.Length < 6)
            errors.Add("Пароль должен быть не менее 6 символов");
        else if (request.Password.Length > 100)
            errors.Add("Пароль слишком длинный");

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add("Фамилия обязательна");
        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add("Имя обязательно");
        if (string.IsNullOrWhiteSpace(request.ContactInfo))
            errors.Add("Контактная информация обязательна");

        await Task.CompletedTask;
        return new ValidationResult(!errors.Any(), errors);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
