namespace LibApp.Core.Exceptions;

public class ValidationException : Exception
{
    public override string Message => new([.. ExceptionDetails.SelectMany(x => $"{x}\n".ToCharArray())]);
    public virtual List<string> ExceptionDetails { get; set; } = [];
}
