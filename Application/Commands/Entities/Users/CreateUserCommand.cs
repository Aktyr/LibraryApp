using LibApp.Core.DTO.Entities;
using LibApp.Core.Requests.Entities.User;

namespace LibApp.Application.Commands.Entities.Users;

public class CreateUserCommand(IRepository<User> userRepo, UserValidatorAsync userValidator, IConverter<User, UserDTO> userConverter)
    : ICreateOrUpdateCommand<CreateUserRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(CreateUserRequest request, CancellationToken cancellationToken)
    {
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
            throw new ValidationException { ExceptionDetails = validationResult.Errors };

        // Добавление
        await userRepo.Add(user, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "User is created.");
    }
}