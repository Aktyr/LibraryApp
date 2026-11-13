namespace LibApp.Application.Services;

/// <summary>
/// Реализация <see cref="IBorrowingService"/>. Делает ровно то, что раньше было
/// в <c>BorrowBookCommand</c>: проверки, создание <see cref="UserRoomBook"/>, инкремент
/// <c>BorrowedCount</c>. Без SaveChanges и без аудита — это ответственность команды,
/// которая открывает транзакцию.
/// </summary>
public class BorrowingService(
    IUnitOfWork unitOfWork,
    BorrowingValidatorAsync validator,
    TimeProvider timeProvider) : IBorrowingService, IService
{
    public async Task<UserRoomBook> BorrowAsync(
        Id userId,
        Id roomBookId,
        int borrowDays,
        Id? borrowRequestId,
        CancellationToken cancellationToken)
    {
        var userRepo = unitOfWork.GetRepository<User>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var userRoomBookRepo = unitOfWork.GetRepository<UserRoomBook>();

        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UserNotFoundException();

        var roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Id == roomBookId, cancellationToken)
            ?? throw new UserRoomBookNotFoundException();

        // Тот же guard, что и раньше: у пользователя не может быть одновременно
        // две активные выдачи одного и того же экземпляра.
        if (await userRoomBookRepo.AnyAsync(
                urb => urb.User.Id == userId
                    && urb.RoomBook.Id == roomBookId
                    && urb.ReturnDate == null,
                cancellationToken))
        {
            throw new LibValidationException
            {
                ExceptionDetails = ["Пользователь уже взял эту книгу и ещё не вернул её"]
            };
        }

        var validationResult = await validator.ValidateBorrowAsync(user, roomBook, borrowDays, cancellationToken);
        if (!validationResult.IsValid)
            throw new LibValidationException { ExceptionDetails = validationResult.Errors };

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var userRoomBook = new UserRoomBook
        {
            Id = new Id(Guid.NewGuid()),
            User = user,
            RoomBook = roomBook,
            BorrowDate = now,
            Deadline = now.AddDays(borrowDays),
            BorrowRequestId = borrowRequestId
        };

        roomBook.BorrowedCount++;
        user.RoomBooks.Add(userRoomBook);

        return userRoomBook;
    }
}