namespace BlazorWeb.Api;

public static class AuthProxyEndpoints
{
    public static void MapAuthProxy(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proxy/auth")
            .WithTags("Proxy");

        // Анонимный: пользователь деактивирован, Identity-сессии нет.
        group.MapPost("/restore-account", RestoreAccountAsync);
    }

    private static async Task<IResult> RestoreAccountAsync(
        SelfRestoreRequest request,
        ISelfRestoreService service,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        try
        {
            await service.SelfRestoreAsync(request.Email, request.Password, ct);
            return Results.Ok(new BasicCreateDeleteResponse("Ok", "Аккаунт восстановлен"));
        }
        catch (UnauthorizedException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 401);
        }
        catch (LibValidationException ex)
        {
            return Results.Problem(
                detail: string.Join("\n", ex.ExceptionDetails),
                statusCode: 400);
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger("AuthProxy")
                .LogError(ex, "Self-restore failed for {Email}", request.Email);
            return Results.Problem(
                detail: "Не удалось восстановить аккаунт, попробуйте позже",
                statusCode: 500);
        }
    }
}