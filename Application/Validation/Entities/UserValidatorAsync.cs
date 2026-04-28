namespace LibApp.Application.Validation.Entities;

public class UserValidatorAsync : IValidator
{
    public async Task<ValidationResponse> ValidateAsync(User user, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(user.LastName))
            errors.Add("Фамилия обязательна");

        if (string.IsNullOrWhiteSpace(user.FirstName))
            errors.Add("Имя обязательно");

        if (string.IsNullOrWhiteSpace(user.ContactInfo))
            errors.Add("Контактная информация обязательна");

        if (user.LastName.Length > 100)
            errors.Add("Фамилия не может превышать 100 символов");

        if (user.FirstName.Length > 100)
            errors.Add("Имя не может превышать 100 символов");

        if (user.MiddleName.Length > 100)
            errors.Add("Отчество не может превышать 100 символов");

        //if (user.ContactInfo.Length > 200)
        //    errors.Add("Контактная информация не может превышать 200 символов");

        await Task.CompletedTask;

        return new ValidationResponse(!errors.Any(), errors);
    }
}