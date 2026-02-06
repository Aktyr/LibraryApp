namespace LibApp.Application.Entities.Users;

public class GetAllUsersQuery(IRepository<User> userRepo)
    : IGetQuery<EmptyRequest, UserResponse>
{
    public async Task<UserResponse> Execute(EmptyRequest emptyRequest, CancellationToken cancellationToken)
    {
        var users = await userRepo.GetWithoutTracking(cancellationToken);
        var userDTOs = users.Select(user => new UserDTO(
            user.Id.Value,
            user.LastName,
            user.FirstName,
            user.MiddleName,
            user.ContactInfo,
            user.NearestReturnTimeSpan
        )).ToArray();

        return new UserResponse("Ok", "List of users issued successfully.", userDTOs);
    }
}