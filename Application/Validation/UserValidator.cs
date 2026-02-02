namespace LibApp.Application.Validation;

public class UserValidator
{
    public static ValidationResult Validate(User user)
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


        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}