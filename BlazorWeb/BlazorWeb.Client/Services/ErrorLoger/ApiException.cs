namespace BlazorWeb.Client.Services;

public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}