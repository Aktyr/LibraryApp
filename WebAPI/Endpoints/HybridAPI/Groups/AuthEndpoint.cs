namespace WebAPI.Endpoints.HybridAPI.Groups;

[Tags("Auth")]
[Route("/api/auth")]
[AllowAnonymous]
public class AuthEndpoint : EndpointBase
{
    [HttpPost("/login")]
    public async Task<IResult> Login(LoginRequest request, LoginCommand command, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));

    [HttpPost("/register")]
    public async Task<IResult> Register(RegisterRequest request, RegisterCommand command, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));
    [HttpPost("/restore-account")]
    public async Task<IResult> RestoreAccount(SelfRestoreRequest request, SelfRestoreCommand command, CancellationToken ct) => Results.Ok(await command.Execute(request, ct));
}
