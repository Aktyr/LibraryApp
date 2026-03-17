namespace LibApp.Application.Validation.Borrowing;

public class BorrowingValidatorAsync
{
    // Параметры валидации
    private const int MAX_BOOKS_PER_USER = 5;
    private const int MIN_BORROW_DAYS = 1;
    private const int MAX_BORROW_DAYS = 30;
    private const int MAX_EXTEND_DAYS = 14;

    /// <summary>
    /// Валидация выдачи книги
    /// </summary>
    public async Task<ValidationResult> ValidateBorrowAsync(User user, RoomBook roomBook, int borrowDays, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (user == null)
            errors.Add("Пользователь не найден");
        else
        {
            var activeBooksCount = user.RoomBooks?.Count(urb => !urb.IsReturned) ?? 0;
            if (activeBooksCount >= MAX_BOOKS_PER_USER)
                errors.Add($"Пользователь уже взял максимальное количество книг ({MAX_BOOKS_PER_USER})");

            // Проверка на просрочки
            var overdueBooks = user.RoomBooks?.Where(urb => !urb.IsReturned && urb.Deadline < DateTime.Now) ?? Enumerable.Empty<UserRoomBook>();
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

    public async Task<ValidationResult> ValidateReturnAsync(UserRoomBook userRoomBook, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (userRoomBook == null)
            errors.Add("Запись о выдаче не найдена");
        else if (userRoomBook.IsReturned)
            errors.Add("Книга уже возвращена");

        await Task.CompletedTask;
        return new ValidationResult(!errors.Any(), errors);
    }

    public async Task<ValidationResult> ValidateExtendAsync(UserRoomBook userRoomBook, int extraDays, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (userRoomBook == null)
            errors.Add("Запись о выдаче не найдена");
        else if (userRoomBook.IsReturned)
            errors.Add("Нельзя продлить уже возвращенную книгу");
        else if (extraDays <= 0 || extraDays > MAX_EXTEND_DAYS)
            errors.Add($"Срок продления должен быть от 1 до {MAX_EXTEND_DAYS} дней");

        await Task.CompletedTask;
        return new ValidationResult(!errors.Any(), errors);
    }

    /// <summary>
    /// Валидация запроса на выдачу (проверка входных данных)
    /// </summary>
    public async Task<ValidationResult> ValidateBorrowRequestAsync(
        BorrowBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (request.UserId == Guid.Empty)
            errors.Add("ID пользователя обязателен");

        if (request.RoomBookId == Guid.Empty)
            errors.Add("ID книги обязателен");

        if (request.BorrowDays < MIN_BORROW_DAYS || request.BorrowDays > MAX_BORROW_DAYS)
            errors.Add($"Срок выдачи должен быть от {MIN_BORROW_DAYS} до {MAX_BORROW_DAYS} дней");

        await Task.CompletedTask;
        return new ValidationResult(!errors.Any(), errors);
    }
}