namespace LibApp.Application.Commands.Entities.Books;

public class DiscardBookCommand(IUnitOfWork unitOfWork) : ICreateOrUpdateCommand<DiscardBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DiscardBookRequest request, CancellationToken ct)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        if (request.Amount <= 0)
            throw new LibValidationException { ExceptionDetails = ["Количество списываемых экземпляров должно быть больше нуля"] };

        // Валидация
        // Находим книгу
        var books = await bookRepo.GetAsync(b => b.Id.Value == request.BookId, ct);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        // Находим RoomBook
        var roomBooks = await roomBookRepo.GetAsync(rb => rb.Book.Id.Value == request.BookId, ct);
        var roomBook = roomBooks.FirstOrDefault();

        if (roomBook == null)
            throw new LibValidationException { ExceptionDetails = ["Книга не найдена в комнатах"] };

        // Проверяем, что списываем не больше, чем есть
        if (request.Amount > roomBook.BookCount)
            throw new LibValidationException { ExceptionDetails = [$"Нельзя списать {request.Amount} экз. Доступно: {roomBook.BookCount}"] };

        // Проверяем, что списываемые экземпляры не выданы
        if (request.Amount > roomBook.AvailableCount)
            throw new LibValidationException { ExceptionDetails = [$"Нельзя списать {request.Amount} экз. Выдано: {roomBook.BorrowedCount}, доступно: {roomBook.AvailableCount}"] };

        //todo возможно заменить проверку на конкретных пользователей
        if (!string.IsNullOrEmpty(request.ApprovedBy) && request.ApprovedBy.Length > 100)
            throw new LibValidationException { ExceptionDetails = ["ApprovedBy не может превышать 100 символов"] };


        //  Создаём запись о списании
        var discarded = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = roomBook.Room,
            RoomId = roomBook.Room.Id,
            Amount = request.Amount,
            DiscardedDate = DateTime.UtcNow,
            DiscardReason = request.DiscardReason,
            ApprovedBy = request.ApprovedBy,
            CompensationAmount = request.CompensationAmount
        };

        // Уменьшаем количество экземпляров
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await discardedRepo.AddAsync(discarded, ct);
            roomBook.BookCount -= request.Amount;
            await roomBookRepo.UpdateAsync(roomBook, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        return ResponseFactory.Success($"Списано {request.Amount} экз. книги '{book.Title}'. Причина: {request.DiscardReason}");
    }
}