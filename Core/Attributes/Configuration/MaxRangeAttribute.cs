namespace LibApp.Core.Attributes.Configuration;

/// <summary>
/// Ограничивает максимальное значение свойства
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MaxRangeAttribute : ValidationAttribute
{
    private readonly double _max;
    public MaxRangeAttribute(double max) => _max = max;   
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) 
            return ValidationResult.Success;

        var numericValue = Convert.ToDouble(value);

        if (numericValue > _max)
            return new ValidationResult($"{validationContext.DisplayName} cannot exceed {_max}. Current value: {numericValue}");        

        return ValidationResult.Success;
    }
}