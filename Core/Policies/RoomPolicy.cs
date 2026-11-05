namespace LibApp.Core.Policies;

/// <summary>
/// Правила валидации комнаты. Единая точка для Application (RoomValidatorAsync)
/// и клиентских черновиков (RoomDraft в Rooms.razor).
/// </summary>
public static class RoomPolicy
{
    public const int MaxNameLength = 100;

    /// <summary>Проверяет параметры комнаты. Возвращает сообщения для UI.</summary>
    public static List<string> Validate(string? name)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Название комнаты обязательно");
        else if (name.Length > MaxNameLength)
            errors.Add($"Название комнаты не может превышать {MaxNameLength} символов");

        return errors;
    }
}