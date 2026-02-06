namespace LibApp.Application.Entities.Users;

public class UpdateUserCommand(IRepository<User> userRepo, UserValidatorAsync userValidator)
    : ICreateOrUpdateCommand<UpdateUserRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var users = await userRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var user = users.FirstOrDefault();

        if (user == null)
            throw new UserNotFoundException();

        // Создаем временного пользователя для валидации
        var userForValidation = new User
        {
            LastName = request.LastName,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            ContactInfo = request.ContactInfo
        };

        // Асинхронная валидация
        var validationResult = await userValidator.ValidateAsync(userForValidation, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // Обновляем только если валидация прошла
        user.LastName = request.LastName;
        user.FirstName = request.FirstName;
        user.MiddleName = request.MiddleName;
        user.ContactInfo = request.ContactInfo;

        await userRepo.Update(user, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "User updated successfully.");
    }
}