namespace LibApp.Core.Exceptions;

public class RoomExistsException(string name) : Exception($"Room with name '{name}' already exists.") { }

public class RoomNotFoundException : Exception
{
    public RoomNotFoundException() : base("Room not found.") { }
    public RoomNotFoundException(string message) : base(message) { }
}

public class RoomDeletionException : Exception
{
    public RoomDeletionException() : base("Room deletion error.") { }
    public RoomDeletionException(string message) : base(message) { }
}
