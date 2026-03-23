namespace LibApp.Core.Exceptions.Auth;

public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("Unauthorized access") { }
    public UnauthorizedException(string message) : base(message) { }
}