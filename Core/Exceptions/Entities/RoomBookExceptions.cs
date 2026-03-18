namespace LibApp.Core.Exceptions.Entities;

public class RoomBookNotFoundException : Exception
{
    public RoomBookNotFoundException() : base("Book is not found in Room.") { }
    public RoomBookNotFoundException(string message) : base(message) { }
}

