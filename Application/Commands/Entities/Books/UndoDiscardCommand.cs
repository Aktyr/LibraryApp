namespace LibApp.Application.Commands.Entities.Books;

/// <summary>
/// Отменяет списание книги: возвращает экземпляры в фонд и удаляет запись о списании.
/// Если книга была помечена как «списанная» (IsDiscarded), флаг снимается.
/// </summary>
public class UndoDiscardCommand(
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : AuditedCreateOrUpdateCommand<UndoDiscardRequest, BasicCreateDeleteResponse>(unitOfWork, auditService)
{
    protected override async Task<(BasicCreateDeleteResponse Response, AuditEntry? Audit)> ExecuteCore(
        UndoDiscardRequest request, CancellationToken ct)
    {
        var discardedRepo = UnitOfWork.GetRepository<DiscardedBook>();
        var roomBookRepo = UnitOfWork.GetRepository<RoomBook>();
        var bookRepo = UnitOfWork.GetRepository<Book>();

        var discardId = new Id(request.DiscardId);
        var discarded = await discardedRepo.FirstOrDefaultAsync(d => d.Id == discardId, ct);
        if (discarded == null)
            throw new LibValidationException { ExceptionDetails = ["Запись о списании не найдена"] };

        var roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Book.Id == discarded.Book.Id && rb.Room.Id == discarded.RoomId, ct);
        if (roomBook == null)
            throw new LibValidationException { ExceptionDetails = ["Книга не найдена в комнате"] };

        roomBook.BookCount += discarded.Amount;
        await roomBookRepo.UpdateAsync(roomBook, ct);
        await discardedRepo.RemoveAsync(discarded, ct);

        // Снимаем флаг «списана», если книга снова есть в наличии
        if (roomBook.Book.IsDiscarded && roomBook.BookCount > 0)
        {
            roomBook.Book.IsDiscarded = false;
            await bookRepo.UpdateAsync(roomBook.Book, ct);
        }

        var audit = new AuditEntry(
            AuditActionType.UndoDiscard,
            nameof(DiscardedBook),
            discardId.Value,
            $"Отменено списание {discarded.Amount} экз. книги «{discarded.Book.Title}» " +
            $"из комнаты «{discarded.Room.Name}». Причина: {request.UndoReason}");

        var response = ResponseFactory.Success(
            $"Отменено списание {discarded.Amount} экз. книги '{discarded.Book.Title}'. Причина: {request.UndoReason}");

        return (response, audit);
    }
}