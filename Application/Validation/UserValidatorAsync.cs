using LibApp.Core.Entities;

namespace LibApp.Application.Validation;

public class UserValidatorAsync
{
    public async Task<ValidationResult> ValidateAsync(User user, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(user.LastName))
            errors.Add("Фамилия обязательна");

        if (string.IsNullOrWhiteSpace(user.FirstName))
            errors.Add("Имя обязательно");

        if (string.IsNullOrWhiteSpace(user.ContactInfo))
            errors.Add("Контактная информация обязательна");

        //if (user.ContactInfo.Length > 200)
        //    errors.Add("Контактная информация не может превышать 200 символов");

        await Task.CompletedTask;

        return new ValidationResult(!errors.Any(), errors);
    }
}