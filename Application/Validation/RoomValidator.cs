namespace LibApp.Application.Validation;

public class RoomValidator
{
    public static ValidationResult Validate(Room room)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(room.Name))
            errors.Add("Название комнаты обязательно");

        if (room.Name.Length < 3 || room.Name.Length > 100)
            errors.Add("Название комнаты должно быть от 3 до 100 символов");

        // Нужна ли проверка на уникальность имени ?

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}