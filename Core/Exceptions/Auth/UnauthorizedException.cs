namespace LibApp.Core.Exceptions.Auth;

public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("Неавторизованный доступ") { }
    public UnauthorizedException(string message) : base(message) { }
}