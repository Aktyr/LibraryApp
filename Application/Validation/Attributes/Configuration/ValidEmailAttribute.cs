namespace LibApp.Application.Validation.Attributes.Configuration;

/// <summary>
/// Проверяет корректность email адреса
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidEmailAttribute : ValidationAttribute
{
    public ValidEmailAttribute() : base("{0} is not a valid email address") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;

        var email = value.ToString();
        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Success;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address == email)
                return ValidationResult.Success;
        }
        catch { }

        return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }
}