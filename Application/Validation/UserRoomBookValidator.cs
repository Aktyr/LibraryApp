namespace LibApp.Application.Validation;

public class UserRoomBookValidator
{
    public static ValidationResult Validate(UserRoomBook userRoomBook)
    {
        var errors = new List<string>();

        if (userRoomBook.Issue > DateTime.Now)
            errors.Add("Дата выдачи не может быть в будущем");

        if (userRoomBook.Deadline.HasValue && userRoomBook.Deadline < userRoomBook.Issue)
            errors.Add("Срок возврата не может быть раньше даты выдачи");

        //if (userRoomBook.Deadline.HasValue && userRoomBook.Deadline > DateTime.Now.AddYears(1))
        //    errors.Add("Срок возврата не может быть более чем через год");

        if (userRoomBook.User == null)
            errors.Add("Пользователь обязателен");

        if (userRoomBook.RoomBook == null)
            errors.Add("Книга обязательна");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}