namespace WebAPI.Endpoints.HybridAPI.Groups;

// Эталон
// атрибутиы для MapGroup("/api/auth"), доступа (если нет аттрибута, то AllowAnonymous()), тегов WithTags(_authGroup);
[Tags("Auth")]
[Route("/api/auth")]
[AllowAnonymous]
public class AuthEndpoint : EndpointBase
{
    // добавить атрибут пути (если надо), доступа (если дополнительные)
    [HttpPost("/login")]
    public async Task<IResult> Login(LoginRequest request, LoginCommand command, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    // добавить атрибут пути (если надо), доступа (если дополнительные)
    [HttpPost("/register")]
    public async Task<IResult> Register(RegisterRequest request, RegisterCommand command, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));
}
