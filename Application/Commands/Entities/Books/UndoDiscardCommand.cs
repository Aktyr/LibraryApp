namespace LibApp.Application.Commands.Entities.Books;

public class UndoDiscardCommand(
        IRepository<DiscardedBook> discardedRepo,
        IRepository<RoomBook> roomBookRepo,
        IRepository<User> userRepo) : ICreateOrUpdateCommand<UndoDiscardRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(UndoDiscardRequest request, CancellationToken ct)
    {
        // Находим запись о списании
        var discarded = (await discardedRepo.Get(d => d.Id.Value == request.DiscardId, ct)).FirstOrDefault()
            ?? throw new LibValidationException { ExceptionDetails = ["Запись о списании не найдена"] };

        // Находим RoomBook
        var roomBooks = await roomBookRepo.Get(rb => rb.Book.Id.Value == discarded.Book.Id.Value
                                                   && rb.Room.Id.Value == discarded.RoomId, ct);
        var roomBook = roomBooks.FirstOrDefault()
            ?? throw new LibValidationException { ExceptionDetails = ["Книга не найдена в комнате"] };

        // Восстанавливаем количество экземпляров
        roomBook.BookCount += discarded.Amount;
        await roomBookRepo.Update(roomBook, ct);

        // Удаляем запись о списании
        await discardedRepo.Remove(discarded, ct);

        return ResponseFactory.Success($"Отменено списание {discarded.Amount} экз. книги '{discarded.Book.Title}'. Причина: {request.UndoReason}");
    }
}
