namespace LibApp.Core.Exceptions;

public class RoomExistsException(string name) : Exception
{
    public override string Message => $"Room with name {name} already exist.";
}
