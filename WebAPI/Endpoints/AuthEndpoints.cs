using WebAPI.Endpoints.Service;

namespace WebAPI.Endpoints;

[Route("api/auth")]
public class AuthEndpoints : BaseEndpoint
{
    public AuthEndpoints(IServiceProvider serviceProvider) : base(serviceProvider) { }

    /// <summary>
    /// Вход пользователя
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        // LoginCommand реализует IGetQuery, используем ExecuteQuery
        var result = await ExecuteQuery<LoginCommand, LoginRequest, LoginResponse>(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Регистрация пользователя
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCommand<RegisterCommand, RegisterRequest, RegisterResponse>(request, cancellationToken);
        return Ok(result);
    }
}