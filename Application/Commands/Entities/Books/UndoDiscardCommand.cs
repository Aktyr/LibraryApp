namespace LibApp.Application.Commands.Entities.Books;

public class UndoDiscardCommand(IUnitOfWork unitOfWork) : ICreateOrUpdateCommand<UndoDiscardRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(UndoDiscardRequest request, CancellationToken ct)
    {
        var discardedRepo = unitOfWork.GetRepository<DiscardedBook>();
        var roomBookRepo = unitOfWork.GetRepository<RoomBook>();

        var discarded = (await discardedRepo.GetAsync(d => d.Id.Value == request.DiscardId, ct)).FirstOrDefault()
            ?? throw new LibValidationException { ExceptionDetails = ["Запись о списании не найдена"] };

        var roomBooks = await roomBookRepo.GetAsync(rb => rb.Book.Id.Value == discarded.Book.Id.Value
                                                   && rb.Room.Id == discarded.RoomId, ct);
        var roomBook = roomBooks.FirstOrDefault()
            ?? throw new LibValidationException { ExceptionDetails = ["Книга не найдена в комнате"] };

        // Обёртываем изменения в транзакцию
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            roomBook.BookCount += discarded.Amount;
            await roomBookRepo.UpdateAsync(roomBook, ct);
            await discardedRepo.RemoveAsync(discarded, ct);

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        return ResponseFactory.Success($"Отменено списание {discarded.Amount} экз. книги '{discarded.Book.Title}'. Причина: {request.UndoReason}");
    }
}