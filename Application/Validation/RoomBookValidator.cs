namespace LibApp.Application.Validation;

public class RoomBookValidator
{
    public static ValidationResult Validate(RoomBook roomBook)
    {
        var errors = new List<string>();

        if (roomBook.BookCount < 0)
            errors.Add("Количество книг не может быть отрицательным");

        //if (roomBook.BookCount > 1000)
        //    errors.Add("Количество книг не может превышать 1000");

        if (roomBook.Room == null)
            errors.Add("Комната обязательна");

        if (roomBook.Book == null)
            errors.Add("Книга обязательна");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}