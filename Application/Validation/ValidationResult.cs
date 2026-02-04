namespace LibApp.Application.Validation;

public record ValidationResult(bool IsValid, List<string> Errors)
{
    public ValidationResult() : this(false, new List<string>()) { }
}