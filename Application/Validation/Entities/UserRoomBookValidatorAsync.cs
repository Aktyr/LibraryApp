namespace LibApp.Application.Validation;

// Возможно валидатор избыточен при наличии BorrowingValidatorAsync
[Obsolete]
public class UserRoomBookValidatorAsync
{
    // Параметры валидации
    private const int MAX_BOOKS_PER_USER = 5;
    private const int MIN_BORROW_DAYS = 1;
    private const int MAX_BORROW_DAYS = 30;

    public async Task<ValidationResult> ValidateAsync(User user, RoomBook roomBook, int borrowDays, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (user == null)
            errors.Add("Пользователь не найден");
        else
        {
            // Проверка лимита книг
            var activeBooksCount = user.RoomBooks?.Count(urb => !urb.IsReturned) ?? 0;
            if (activeBooksCount >= MAX_BOOKS_PER_USER)
                errors.Add($"Пользователь уже взял максимальное количество книг ({MAX_BOOKS_PER_USER})");

            // Проверка на просрочки
            var overdueBooks = user.RoomBooks?.Where(urb => !urb.IsReturned && urb.DueDate < DateTime.Now) ?? Enumerable.Empty<UserRoomBook>();
            if (overdueBooks.Any())
                errors.Add("У пользователя есть просроченные книги");
        }

        if (roomBook == null)
            errors.Add("Книга не найдена");
        else
        {
            if (roomBook.AvailableCount <= 0)
                errors.Add("Нет доступных экземпляров книги");
        }

        // Проверка срока выдачи
        if (borrowDays < MIN_BORROW_DAYS || borrowDays > MAX_BORROW_DAYS)
            errors.Add($"Срок выдачи должен быть от {MIN_BORROW_DAYS} до {MAX_BORROW_DAYS} дней");

        await Task.CompletedTask;
        return new ValidationResult(!errors.Any(), errors);
    }
}