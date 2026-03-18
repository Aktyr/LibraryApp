namespace LibApp.Core.Responses.Auth;

public record RegisterResponse(string Status, string Message, string Token, string Email, UserRole Role, Guid UserId) : IAddOrUpdateResponse;