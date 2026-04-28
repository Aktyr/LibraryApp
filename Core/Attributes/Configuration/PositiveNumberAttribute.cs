namespace LibApp.Core.Attributes.Configuration;

/// <summary>
/// Проверяет что значение больше 0
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PositiveNumberAttribute : ValidationAttribute
{
    public PositiveNumberAttribute() : base("{0} must be greater than 0") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;

        try
        {
            // Конвертируем в decimal (поддерживает все числовые типы)
            decimal numericValue = Convert.ToDecimal(value);
            bool isValid = numericValue > 0;

            return isValid
                ? ValidationResult.Success
                : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }
        catch (InvalidCastException)
        {
            // Не числовой тип - пропускаем или возвращаем ошибку
            return ValidationResult.Success;
        }
    }
}