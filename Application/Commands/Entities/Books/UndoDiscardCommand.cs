namespace LibApp.Application.Commands.Entities.Books;

/// <summary>
/// Отменяет списание книги: возвращает экземпляры в фонд и удаляет запись о списании.
/// Если книга была помечена как «списанная» (IsDiscarded), флаг снимается.
/// </summary>
public class UndoDiscardCommand(
    IUnitOfWork unitOfWork,
    IAuditService auditService) : ICreateOrUpdateCommand<UndoDiscardRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(UndoDiscardRequest request, CancellationToken ct)
    {
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();
        var bookRepo = unitOfWork.GetRepository<Book>();

        var discardId = new Id(request.DiscardId);
        var discarded = await discardedRepo.FirstOrDefaultAsync(d => d.Id == discardId, ct);
        if (discarded == null)
            throw new LibValidationException { ExceptionDetails = ["Запись о списании не найдена"] };

        var roomBook = await roomBookRepo.FirstOrDefaultAsync(rb => rb.Book.Id == discarded.Book.Id && rb.Room.Id == discarded.RoomId, ct);
        if (roomBook == null)
            throw new LibValidationException { ExceptionDetails = ["Книга не найдена в комнате"] };

        var amount = discarded.Amount;
        var bookTitle = discarded.Book.Title;
        var bookId = discarded.Book.Id.Value;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            roomBook.BookCount += amount;
            await roomBookRepo.UpdateAsync(roomBook, ct);
            await discardedRepo.RemoveAsync(discarded, ct);

            // Снимаем флаг «списана», если книга снова есть в наличии
            if (roomBook.Book.IsDiscarded && roomBook.BookCount > 0)
            {
                roomBook.Book.IsDiscarded = false;
                await bookRepo.UpdateAsync(roomBook.Book, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogAsync(
                AuditActionType.UndoDiscard,
                nameof(DiscardedBook),
                discardId.Value,
                details: $"Отменено списание {amount} экз. книги «{bookTitle}». Причина: {request.UndoReason}",
                ct: ct);
        }, ct);

        return ResponseFactory.Success($"Отменено списание {amount} экз. книги '{bookTitle}'. Причина: {request.UndoReason}");
    }
}