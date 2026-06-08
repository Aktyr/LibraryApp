namespace WebAPI.Endpoints;

[Route("api/auth")]
public class AuthEndpoints : BaseApiController
{
    public AuthEndpoints(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await ExecuteQuery<LoginCommand, LoginRequest, LoginResponse>(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<RegisterCommand, RegisterRequest, RegisterResponse>(request, cancellationToken);
        return Ok(result);
    }
}