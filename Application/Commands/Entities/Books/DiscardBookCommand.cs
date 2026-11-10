namespace LibApp.Application.Commands.Entities.Books;

/// <summary>
/// Списывает экземпляры книги из фонда конкретной комнаты.
/// Уменьшает BookCount, создаёт запись DiscardedBook и фиксирует действие в аудите.
/// </summary>
public class DiscardBookCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<DiscardBookRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        DiscardBookRequest request, CancellationToken ct)
    {
        var bookRepo = UnitOfWork.GetRepository<Book>();
        var discardedRepo = UnitOfWork.GetRepository<DiscardedBook>();
        var roomBookRepo = UnitOfWork.GetRepository<RoomBook>();

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

        await discardedRepo.AddAsync(discarded, ct);

        roomBook.BookCount -= request.Amount;
        await roomBookRepo.UpdateAsync(roomBook, ct);

        // Промежуточный SaveChanges: проверка anyRemaining должна видеть актуальный
        // BookCount в БД, а не значения в tracking-кэше. Финальный SaveChanges (с аудитом)
        // сделает база перед коммитом.
        await UnitOfWork.SaveChangesAsync(ct);

        // Если это был последний экземпляр во всех комнатах — помечаем книгу списанной.
        var anyRemaining = await roomBookRepo.AnyAsync(
            rb => rb.Book.Id == bookId && rb.BookCount > 0, ct);

        if (!anyRemaining)
        {
            book.IsDiscarded = true;
            await bookRepo.UpdateAsync(book, ct);
        }

        var details = new StringBuilder();
        details.Append($"Списано {request.Amount} экз. «{book.Title}». ");
        details.Append($"Причина: {request.DiscardReason.DisplayName()}");

        var approvedBy = request.ApprovedBy ?? currentUser.FullName;
        if (!string.IsNullOrWhiteSpace(approvedBy))
            details.Append($". Утвердил: {approvedBy}");

        if (request.CompensationAmount is > 0)
            details.Append($". Компенсация: {request.CompensationAmount.Value:0.##} ₽");

        var audit = new AuditEntry(
            AuditActionType.Discard,
            nameof(Book),
            book.Id.Value,
            details.ToString());

        var response = ResponseFactory.Success(
            $"Списано {request.Amount} экз. книги '{book.Title}'. Причина: {request.DiscardReason.DisplayName()}");

        return (response, audit);
    }
}