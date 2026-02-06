namespace LibApp.Application.Entities.Users;

public class CreateUserCommand(IRepository<User> userRepo, UserValidatorAsync userValidator)
    : ICreateOrUpdateCommand<CreateUserRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Валидация
        var validationResult = await userValidator.ValidateAsync(new User
        {
            LastName = request.LastName,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            ContactInfo = request.ContactInfo
        }, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // Создание 
        var user = new User
        {
            LastName = request.LastName,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            ContactInfo = request.ContactInfo,
            RoomBooks = []
        };

        await userRepo.Add(user, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "User is created.");
    }
}