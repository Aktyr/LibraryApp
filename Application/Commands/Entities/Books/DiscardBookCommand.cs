namespace LibApp.Application.Commands.Entities.Books;

/// <summary>
/// Списывает экземпляры книги из фонда конкретной комнаты.
/// Уменьшает BookCount, создаёт запись DiscardedBook и фиксирует действие в аудите.
/// </summary>
public class DiscardBookCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : ICreateOrUpdateCommand<DiscardBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DiscardBookRequest request, CancellationToken ct)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var bookId = new Id(request.BookId);
        var book = await bookRepo.FirstOrDefaultAsync(b => b.Id == bookId, ct);
        if (book == null) throw new BookNotFoundException();

        RoomBook? roomBook;
        if (request.RoomId.HasValue)
        {
            var roomId = new Id(request.RoomId.Value);
            roomBook = await roomBookRepo.FirstOrDefaultAsync(
                rb => rb.Book.Id == bookId && rb.Room.Id == roomId, ct);
            if (roomBook == null)
                throw new LibValidationException { ExceptionDetails = ["Книга не найдена в указанной комнате"] };
        }
        else
        {
            roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Book.Id == bookId, ct);
            if (roomBook == null)
                throw new LibValidationException { ExceptionDetails = ["Книга не найдена в комнатах"] };
        }

        var errors = DiscardPolicy.ValidateDiscard(
            request.Amount,
            roomBook.BookCount,
            roomBook.AvailableCount,
            request.CompensationAmount,
            request.DiscardReason,
            request.ApprovedBy);

        if (errors.Any())
            throw new LibValidationException { ExceptionDetails = errors };

        // Создаём запись о списании
        var discarded = new DiscardedBook
        {
            Id = new Id(Guid.NewGuid()),
            Book = book,
            Room = roomBook.Room,
            RoomId = roomBook.Room.Id,
            Amount = request.Amount,
            DiscardedDate = DateTime.UtcNow,
            DiscardReason = request.DiscardReason,
            ApprovedById = currentUser.LibraryUserId,
            ApprovedBy = request.ApprovedBy ?? currentUser.FullName,
            CompensationAmount = request.CompensationAmount
        };

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await discardedRepo.AddAsync(discarded, ct);

            roomBook.BookCount -= request.Amount;
            await roomBookRepo.UpdateAsync(roomBook, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // Если это был последний экземпляр во всех комнатах — помечаем книгу списанной.
            // Проверка идёт после SaveChanges, чтобы видеть актуальный BookCount в БД.
            var anyRemaining = await roomBookRepo.AnyAsync(
                rb => rb.Book.Id == bookId && rb.BookCount > 0, ct);

            if (!anyRemaining)
            {
                book.IsDiscarded = true;
                await bookRepo.UpdateAsync(book, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }

            await auditService.LogAsync(
                AuditActionType.Update,
                nameof(Book),
                book.Id.Value,
                details: $"Списано {request.Amount} экз. «{book.Title}». Причина: {request.DiscardReason.DisplayName()}",
                ct: ct);
        }, ct);

        return ResponseFactory.Success(
            $"Списано {request.Amount} экз. книги '{book.Title}'. Причина: {request.DiscardReason.DisplayName()}");
    }
}