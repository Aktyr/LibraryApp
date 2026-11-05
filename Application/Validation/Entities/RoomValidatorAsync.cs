namespace LibApp.Application.Validation.Entities;

public class RoomValidatorAsync : IValidator
{
    public async Task<ValidationResponse> ValidateAsync(Room room, CancellationToken cancellationToken = default)
    {
        var errors = RoomPolicy.Validate(room.Name);

        await Task.CompletedTask;

        return new ValidationResponse(errors.Count == 0, errors);
    }
}