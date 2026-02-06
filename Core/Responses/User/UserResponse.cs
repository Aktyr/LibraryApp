namespace LibApp.Core.Responses.User;

public record UserResponse(string Status, string Message, UserDTO[] Users) : IGetResponse;
public record UserDTO(Guid Id,
                      string LastName,
                      string FirstName,
                      string MiddleName,
                      string ContactInfo, 
                      TimeSpan? NearestReturnTimeSpan);