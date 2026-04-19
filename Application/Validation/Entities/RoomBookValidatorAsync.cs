namespace LibApp.Application.Validation.Entities;

public class RoomBookValidatorAsync : IValidator
{
    public async Task<ValidationResult> ValidateAsync(RoomBook roomBook, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (roomBook.BookCount < 0)
            errors.Add("Количество книг не может быть отрицательным");

        if (roomBook.Room == null)
            errors.Add("Комната обязательна");

        if (roomBook.Book == null)
            errors.Add("Книга обязательна");

        //if (roomBook.BookCount > 1000)
        //    errors.Add("Количество книг не может превышать 1000");

        await Task.CompletedTask;

        return new ValidationResult(!errors.Any(), errors);
    }
}