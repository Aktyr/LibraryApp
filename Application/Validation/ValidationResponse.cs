namespace LibApp.Application.Validation;

public record ValidationResponse(bool IsValid, List<string> Errors)
{
    public ValidationResponse() : this(false, new List<string>()) { }
}