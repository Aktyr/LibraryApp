namespace LibApp.Application.Commands.Entities.Books;

public class DiscardBookCommand : ICreateOrUpdateCommand<DiscardBookRequest, BasicCreateDeleteResponse>, ICommand
{
    private readonly IRepository<Book> _bookRepo;
    private readonly IRepository<DiscardedBook> _discardedRepo;
    private readonly IRepository<RoomBook> _roomBookRepo;

    public DiscardBookCommand(
        IRepository<Book> bookRepo,
        IRepository<DiscardedBook> discardedRepo,
        IRepository<RoomBook> roomBookRepo)
    {
        _bookRepo = bookRepo;
        _discardedRepo = discardedRepo;
        _roomBookRepo = roomBookRepo;
    }

    public async Task<BasicCreateDeleteResponse> Execute(DiscardBookRequest request, CancellationToken ct)
    {
        // Валидация
        // Находим книгу
        var books = await _bookRepo.Get(b => b.Id.Value == request.BookId, ct);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        // Находим RoomBook
        var roomBooks = await _roomBookRepo.Get(rb => rb.Book.Id.Value == request.BookId, ct);
        var roomBook = roomBooks.FirstOrDefault();

        if (roomBook == null)
            throw new LibValidationException { ExceptionDetails = ["Книга не найдена в комнатах"] };

        // Проверяем, что списываем не больше, чем есть
        if (request.Quantity > roomBook.BookCount)
            throw new LibValidationException { ExceptionDetails = [$"Нельзя списать {request.Quantity} экз. Доступно: {roomBook.BookCount}"] };

        // Проверяем, что списываемые экземпляры не выданы
        if (request.Quantity > roomBook.AvailableCount)
            throw new LibValidationException { ExceptionDetails = [$"Нельзя списать {request.Quantity} экз. Выдано: {roomBook.BorrowedCount}, доступно: {roomBook.AvailableCount}"] };
        
        //todo возможно заменить проверку на конкретных пользователей
        if (!string.IsNullOrEmpty(request.ApprovedBy) && request.ApprovedBy.Length > 100)
            throw new LibValidationException { ExceptionDetails = ["ApprovedBy не может превышать 100 символов"] };


        //  Создаём запись о списании
        var discarded = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = roomBook.Room,
            RoomId = roomBook.Room.Id.Value,
            Amount = request.Quantity,
            DiscardedDate = DateTime.Now,
            DiscardReason = request.DiscardReason,
            ApprovedBy = request.ApprovedBy,
            CompensationAmount = request.CompensationAmount
        };

        await _discardedRepo.Add(discarded, ct);

        // Уменьшаем количество экземпляров
        roomBook.BookCount -= request.Quantity;
        await _roomBookRepo.Update(roomBook, ct);

        return ResponseFactory.Success($"Списано {request.Quantity} экз. книги '{book.Title}'. Причина: {request.DiscardReason}");
    }
}