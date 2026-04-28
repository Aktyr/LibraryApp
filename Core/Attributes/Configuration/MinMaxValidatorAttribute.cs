namespace LibApp.Core.Attributes.Configuration;

/// <summary>
/// Проверяет что MinProperty &lt; MaxProperty.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MinMaxValidatorAttribute : ValidationAttribute
{
    private readonly string _minPropertyName;
    private readonly string _maxPropertyName;

    public MinMaxValidatorAttribute(string minPropertyName, string maxPropertyName)
    {
        _minPropertyName = minPropertyName;
        _maxPropertyName = maxPropertyName;
    }
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {        
        // Получаем значение валидируемого объекта по имени
        var minProp = validationContext.ObjectType.GetProperty(_minPropertyName);
        var maxProp = validationContext.ObjectType.GetProperty(_maxPropertyName);

        if (minProp?.GetValue(value) is not IComparable minValue || 
            maxProp?.GetValue(value) is not IComparable maxValue)
            return new ValidationResult($"Invalid or missing properties: {_minPropertyName}, {_maxPropertyName}");

        // Гарантируем сравнение одинаковых типов данных
        if (minValue.GetType() != 
            maxValue.GetType())
            return new ValidationResult($"Property types mismatch: {_minPropertyName} ({minValue.GetType()}) vs {_maxPropertyName} ({maxValue.GetType()})");

        // Проверяем чтобы minValue был строго меньше maxValue
        if (minValue.CompareTo(maxValue) >= 0)
            return new ValidationResult($"{_minPropertyName} ({minValue}) must be less than {_maxPropertyName} ({maxValue})");

        return ValidationResult.Success;
    }
}