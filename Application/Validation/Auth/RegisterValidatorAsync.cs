namespace LibApp.Application.Validation.Auth;

public class RegisterValidatorAsync : IValidator // По сути расширяет UserValidatorAsync
{
    private readonly EmailValidatorAsync _emailValidator;
    public RegisterValidatorAsync()
    {
        _emailValidator = new EmailValidatorAsync();
    }

    public async Task<ValidationResponse> ValidateAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Валидация Email
        var emailValidation = await _emailValidator.ValidateAsync(request.Email, cancellationToken);
        if (!emailValidation.IsValid)
            errors.AddRange(emailValidation.Errors);


        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Пароль обязателен");
        else if (request.Password.Length < 6)
            errors.Add("Пароль должен быть не менее 6 символов");
        else if (request.Password.Length > 100)
            errors.Add("Пароль слишком длинный");   
        //todo улучшить условия пароля

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add("Фамилия обязательна");
        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add("Имя обязательно");
        if (string.IsNullOrWhiteSpace(request.ContactInfo))
            errors.Add("Контактная информация обязательна");

        await Task.CompletedTask;
        return new ValidationResponse(!errors.Any(), errors);
    }
}
