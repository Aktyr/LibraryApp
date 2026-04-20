namespace LibApp.Core.Responses.Entities;
public record UserResponse(string Status, string Message, UserDTO[] Users) : IGetResponse;
