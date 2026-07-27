namespace LibApp.Application.Commands.Entities.Users;

// todo разделить создание и регистрацию пользователя. Теряется пароль и логин?
public class CreateUserCommand(
    IUnitOfWork unitOfWork,
    UserValidatorAsync userValidator,
    IConverter<User, UserDTO> userConverter) : ICreateOrUpdateCommand<CreateUserRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        // Создание
        var userDto = new UserDTO(Guid.NewGuid(),
                              request.LastName,
                              request.FirstName,
                              request.MiddleName,
                              request.ContactInfo,
                              null, []);

        var user = userConverter.ToEntity(userDto);

        // Валидация
        var validationResult = await userValidator.ValidateAsync(user, cancellationToken);

        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        // Добавление
        await userRepo.Add(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Created<User>();
    }
}