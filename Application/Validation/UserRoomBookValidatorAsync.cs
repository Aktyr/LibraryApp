namespace LibApp.Application.Validation;

public class UserRoomBookValidatorAsync
{
    public async Task<ValidationResult> ValidateAsync(UserRoomBook userRoomBook, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (userRoomBook.Issue > DateTime.Now)
            errors.Add("Дата выдачи не может быть в будущем");

        if (userRoomBook.Deadline.HasValue && userRoomBook.Deadline < userRoomBook.Issue)
            errors.Add("Срок возврата не может быть раньше даты выдачи");

        if (userRoomBook.User == null)
            errors.Add("Пользователь обязателен");

        if (userRoomBook.RoomBook == null)
            errors.Add("Книга обязательна");

        //if (userRoomBook.Deadline.HasValue && userRoomBook.Deadline > DateTime.Now.AddYears(1))
        //    errors.Add("Срок возврата не может быть более чем через год");

        await Task.CompletedTask;

        return new ValidationResult(!errors.Any(), errors);
    }
}