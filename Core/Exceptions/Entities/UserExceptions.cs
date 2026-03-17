namespace LibApp.Core.Exceptions.Entities;

public class UserNotFoundException : Exception
{
    public UserNotFoundException() : base("User not found") { }
    public UserNotFoundException(string message) : base(message) { }
}
