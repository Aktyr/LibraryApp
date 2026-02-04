namespace LibApp.Core.Exceptions;

public class ValidationException : Exception
{
    public override string Message => string.Join("\n", ExceptionDetails);
    public virtual List<string> ExceptionDetails { get; set; } = [];
}
