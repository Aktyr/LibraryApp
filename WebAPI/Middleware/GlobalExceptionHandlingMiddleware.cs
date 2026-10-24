namespace WebAPI.Middleware;


// todo решить дублирование кода
public class GlobalExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    public GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger) => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, ex.Message);

        var (statusCode, title, detail) = ex switch
        {
            LibValidationException lve =>
                (400, "Validation Error", string.Join("; ", lve.ExceptionDetails)),

            BadHttpRequestException bad =>
                (400, "Bad Request", bad.Message),

            UnauthorizedException =>
                (401, "Unauthorized", ex.Message),

            BookNotFoundException
                or RoomNotFoundException
                or UserNotFoundException
                or RoomBookNotFoundException
                or UserRoomBookNotFoundException
                => (404, "Not Found", ex.Message),

            RoomExistsException
                or RoomDeletionException
                => (409, "Conflict", ex.Message),

            _ => (500, "Server Error", "An internal server error occurred")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Type = title.Replace(' ', '-'),
            Title = title,
            Detail = detail
        };

        var json = JsonSerializer.Serialize(problem);
        await context.Response.WriteAsync(json);
    }
}