namespace LibApp.Core.Responses.Auth;

public record LoginResponse(string Status, string Message, string Token, string Email, UserRole Role, Guid UserId) : IGetResponse;

