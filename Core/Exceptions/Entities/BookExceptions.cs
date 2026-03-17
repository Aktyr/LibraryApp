namespace LibApp.Core.Exceptions.Entities;

public class BookNotFoundException : Exception
{
    public BookNotFoundException() : base("Book not found.") { }
    public BookNotFoundException(string message) : base(message) { }
}