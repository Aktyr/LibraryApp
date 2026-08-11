namespace WebAPI.Middleware;

// todo сделать обработчик для всех исключений
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
            _logger.LogError(ex, ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            ProblemDetails problem = new()
            {
                Status = context.Response.StatusCode,
                Type = "Server Error",
                Title = "Server Error",
                Detail = "An internal server error occurred"
            };

            string json = JsonSerializer.Serialize(problem);

            await context.Response.WriteAsync(json);
        }
    }
}