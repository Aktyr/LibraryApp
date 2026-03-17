using LibApp.Core.DTO.Entities;
using LibApp.Core.Requests.Entities.User;

namespace LibApp.Application.Commands.Entities.Users;

public class UpdateUserCommand(IRepository<User> userRepo, UserValidatorAsync userValidator, IConverter<User, UserDTO> userConverter)
    : ICreateOrUpdateCommand<UpdateUserRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var users = await userRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var user = users.FirstOrDefault() ?? throw new UserNotFoundException();

        // Временное DTO для валидации
        var userDto = new UserDTO(request.Id.Value,
                              request.LastName,
                              request.FirstName,
                              request.MiddleName,
                              request.ContactInfo,
                              user.NearestReturnTimeSpan, []);

        var userForValidation = userConverter.ToEntity(userDto);

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