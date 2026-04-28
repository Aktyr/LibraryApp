namespace LibApp.Core.Attributes.Configuration;

/// <summary>
/// Ограничивает минимальное значение свойства
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MinRangeAttribute : ValidationAttribute
{
    private readonly double _min;

    public MinRangeAttribute(double min) => _min = min;    

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) 
            return ValidationResult.Success;

        var numericValue = Convert.ToDouble(value);

        if (numericValue < _min) 
            return new ValidationResult($"{validationContext.DisplayName} must be at least {_min}. Current value: {numericValue}");        

        return ValidationResult.Success;
    }
}