namespace LibApp.Core.Exceptions.Borrowing;

public class RoomBookNotFoundException : Exception
{
    public RoomBookNotFoundException() : base("Book not found in room") { }
    public RoomBookNotFoundException(string message) : base(message) { }
}

public class UserRoomBookNotFoundException : Exception
{
    public UserRoomBookNotFoundException() : base("Issue record not found") { }
    public UserRoomBookNotFoundException(string message) : base(message) { }
}
