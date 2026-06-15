namespace LibApp.Application.Commands.Entities.Books;

public class UndoDiscardCommand : ICreateOrUpdateCommand<UndoDiscardRequest, BasicCreateDeleteResponse>, ICommand
{
    private readonly IRepository<DiscardedBook> _discardedRepo;
    private readonly IRepository<RoomBook> _roomBookRepo;
    private readonly IRepository<User> _userRepo;

    public UndoDiscardCommand(
        IRepository<DiscardedBook> discardedRepo,
        IRepository<RoomBook> roomBookRepo,
        IRepository<User> userRepo)
    {
        _discardedRepo = discardedRepo;
        _roomBookRepo = roomBookRepo;
        _userRepo = userRepo;
    }

    public async Task<BasicCreateDeleteResponse> Execute(UndoDiscardRequest request, CancellationToken ct)
    {
        // Находим запись о списании
        var discarded = (await _discardedRepo.Get(d => d.Id.Value == request.DiscardId, ct)).FirstOrDefault()
            ?? throw new LibValidationException { ExceptionDetails = ["Запись о списании не найдена"] };

        // Проверяем кто отменяет
        //if (!string.IsNullOrEmpty(request.ApprovedBy))
        //{
        //    var approver = (await _userRepo.Get(u => u.FullName == request.ApprovedBy
        //                                          && (u.Role == UserRole.Librarian || u.Role == UserRole.Admin), ct))
        //                                          .FirstOrDefault();
        //    if (approver == null)
        //        throw new LibValidationException { ExceptionDetails = ["Отменяющий не найден или не имеет прав"] };
        //}

        // Находим RoomBook
        var roomBooks = await _roomBookRepo.Get(rb => rb.Book.Id.Value == discarded.Book.Id.Value
                                                   && rb.Room.Id.Value == discarded.RoomId, ct);
        var roomBook = roomBooks.FirstOrDefault()
            ?? throw new LibValidationException { ExceptionDetails = ["Книга не найдена в комнате"] };

        // Восстанавливаем количество экземпляров
        roomBook.BookCount += discarded.Amount;
        await _roomBookRepo.Update(roomBook, ct);

        // Удаляем запись о списании (или помечаем как отменённую)
        await _discardedRepo.Remove(discarded, ct);

        return new BasicCreateDeleteResponse("Ok",
            $"Отменено списание {discarded.Amount} экз. книги '{discarded.Book.Title}'. Причина: {request.UndoReason}");
    }
}
