namespace LibApp.Core.Exceptions;

public class BookNotFoundException : Exception
{
    public BookNotFoundException() : base("Book not found.") { }
    public BookNotFoundException(string message) : base(message) { }
}