namespace LibApp.Application.Validation.Borrowing;

public class BorrowingValidatorAsync : IValidator
{
    private readonly BorrowingSettings _settings;
    public BorrowingValidatorAsync(IOptionsSnapshot<BorrowingSettings> settings) => _settings = settings.Value;

    /// <summary>
    /// Валидация выдачи книги
    /// </summary>
    public async Task<ValidationResponse> ValidateBorrowAsync(User user, RoomBook roomBook, int borrowDays, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (user == null)
            errors.Add("Пользователь не найден");
        else
        {
            if (user.SubscriptionStatus != SubscriptionStatus.Active)
            {
                var statusName = user.SubscriptionStatus == SubscriptionStatus.Unconfirmed
                    ? "не подтверждён" : "заморожен";
                errors.Add($"Профиль пользователя {statusName}. Выдача книг запрещена.");
            }

            var activeBooksCount = user.RoomBooks?.Count(urb => !urb.IsReturned) ?? 0;
            if (activeBooksCount >= _settings.MaxBooksPerUser)
                errors.Add($"Пользователь уже взял максимальное количество книг ({_settings.MaxBooksPerUser})");

            var overdueBooks = user.RoomBooks?.Where(urb => !urb.IsReturned && urb.Deadline < DateTime.UtcNow) ?? Enumerable.Empty<UserRoomBook>();
            if (overdueBooks.Any())
                errors.Add("У пользователя есть просроченные книги");
        }

        if (roomBook == null)
            errors.Add("Книга не найдена");
        else if (roomBook.AvailableCount <= 0)
            errors.Add("Нет доступных экземпляров книги");

        errors.AddRange(BorrowingPolicy.ValidateBorrow(borrowDays, _settings.MinBorrowDays, _settings.MaxBorrowDays));

        await Task.CompletedTask;
        return new ValidationResponse(!errors.Any(), errors);
    }

    public async Task<ValidationResponse> ValidateExtendAsync(UserRoomBook userRoomBook, int extraDays, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (userRoomBook == null)
            errors.Add("Запись о выдаче не найдена");
        else if (userRoomBook.IsReturned)
            errors.Add("Нельзя продлить уже возвращенную книгу");
        else
        {
            errors.AddRange(BorrowingPolicy.ValidateExtend(userRoomBook.TotalExtendedDays, extraDays, _settings.MaxExtendDeadlineDays));
        }

        await Task.CompletedTask;
        return new ValidationResponse(!errors.Any(), errors);
    }

    public async Task<ValidationResponse> ValidateReturnAsync(UserRoomBook userRoomBook, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (userRoomBook == null)
            errors.Add("Запись о выдаче не найдена");
        else if (userRoomBook.IsReturned)
            errors.Add("Книга уже возвращена");

        await Task.CompletedTask;
        return new ValidationResponse(!errors.Any(), errors);
    }

    /// <summary>
    /// Валидация запроса на выдачу (проверка входных данных)
    /// </summary>
    public async Task<ValidationResponse> ValidateBorrowRequestAsync(BorrowBookRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (request.UserId == Guid.Empty)
            errors.Add("ID пользователя обязателен");

        if (request.RoomBookId == Guid.Empty)
            errors.Add("ID книги обязателен");

        errors.AddRange(BorrowingPolicy.ValidateBorrow(request.BorrowDays, _settings.MinBorrowDays, _settings.MaxBorrowDays));

        await Task.CompletedTask;
        return new ValidationResponse(!errors.Any(), errors);
    }
}