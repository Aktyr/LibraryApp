namespace WebAPI.Endpoints.Service;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = GetErrorResponse(exception);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsJsonAsync(response, jsonOptions);
    }

    private static (int StatusCode, object Response) GetErrorResponse(Exception exception)
    {
        return exception switch
        {
            LibValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                (object)new { errors = validationEx.ExceptionDetails }
            ),
            UnauthorizedException authEx => (
                StatusCodes.Status401Unauthorized,
                new { message = authEx.Message }
            ),
            BookNotFoundException => (
                StatusCodes.Status404NotFound,
                new { message = "Book not found" }
            ),
            RoomNotFoundException => (
                StatusCodes.Status404NotFound,
                new { message = "Room not found" }
            ),
            UserNotFoundException => (
                StatusCodes.Status404NotFound,
                new { message = "User not found" }
            ),
            UserRoomBookNotFoundException => (
                StatusCodes.Status404NotFound,
                new { message = "Borrowing record not found" }
            ),
            RoomBookNotFoundException => (
                StatusCodes.Status404NotFound,
                new { message = "Book is not found in Room" }
            ),
            RoomExistsException existsEx => (
                StatusCodes.Status409Conflict,
                new { message = existsEx.Message }
            ),
            RoomDeletionException deletionEx => (
                StatusCodes.Status409Conflict,
                new { message = deletionEx.Message }
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while processing your request" }
            )
        };
    }
}