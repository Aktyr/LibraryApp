namespace LibApp.Application.Commands.Entities.Books;

public class DiscardBookCommand(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
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
            roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Book.Id == bookId && rb.Room.Id == roomId, ct);
            if (roomBook == null)
                throw new LibValidationException { ExceptionDetails = ["Книга не найдена в указанной комнате"] };
        }
        else
        {
            roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Book.Id == bookId, ct);
            if (roomBook == null)
                throw new LibValidationException { ExceptionDetails = ["Книга не найдена в комнатах"] };
        }

        // ВАЛИДАЦИЯ ЧЕРЕЗ ПОЛИТИКУ
        var errors = DiscardPolicy.ValidateDiscard(
            request.Amount,
            roomBook.BookCount,
            roomBook.AvailableCount,
            request.CompensationAmount,
            request.DiscardReason);

        if (!string.IsNullOrEmpty(request.ApprovedBy) && request.ApprovedBy.Length > DiscardPolicy.MaxApprovedByNameLength)
            errors.Add($"ApprovedBy не может превышать {DiscardPolicy.MaxApprovedByNameLength} символов");

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

        // Уменьшаем количество экземпляров
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await discardedRepo.AddAsync(discarded, ct);
            roomBook.BookCount -= request.Amount;
            await roomBookRepo.UpdateAsync(roomBook, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // Если после списания не осталось ни одного экземпляра ни в одной комнате — помечаем книгу списанной
            var anyRemaining = await roomBookRepo.AnyAsync(
                rb => rb.Book.Id == bookId && rb.BookCount > 0, ct);

            if (!anyRemaining)
            {
                book.IsDiscarded = true;
                await bookRepo.UpdateAsync(book, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }

            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        return ResponseFactory.Success($"Списано {request.Amount} экз. книги '{book.Title}'. Причина: {request.DiscardReason.DisplayName()}");
    }


}