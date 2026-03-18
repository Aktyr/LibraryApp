namespace LibApp.Core.Exceptions.Entities;

public class UserRoomBookNotFoundException : Exception
{
    public UserRoomBookNotFoundException() : base("Borrowing record not found") { }
    public UserRoomBookNotFoundException(string message) : base(message) { }
}
