namespace LibApp.Core.Responses.User;

public record UserResponse(string Status, string Message, UserDTO[] Users) : IGetResponse;
