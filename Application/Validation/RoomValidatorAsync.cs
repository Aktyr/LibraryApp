namespace LibApp.Application.Validation;

public class RoomValidatorAsync
{
    public async Task<ValidationResult> ValidateAsync(Room room, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(room.Name))
            errors.Add("Название комнаты обязательно");

        if (room.Name.Length > 100)
            errors.Add("Название комнаты не может превышать 100 символов");

        await Task.CompletedTask;

        return new ValidationResult(!errors.Any(), errors);
    }

}
