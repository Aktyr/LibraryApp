namespace LibApp.Application.Entities.Users;

public class GetUserQuery(IRepository<User> userRepo)
    : IGetQuery<GetUserRequest, UserResponse>
{
    public async Task<UserResponse?> Execute(GetUserRequest request, CancellationToken cancellationToken)
    {
        var users = await userRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var user = users.FirstOrDefault();

        if (user == null)
            throw new UserNotFoundException();

        return new UserResponse("Ok", "User issued successfully.", [new(
            user.Id.Value,
            user.LastName,
            user.FirstName,
            user.MiddleName,
            user.ContactInfo,
            user.NearestReturnTimeSpan)]
        );
    }
}