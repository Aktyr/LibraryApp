namespace LibApp.Application.Commands.Entities.Rooms;

public class UpdateRoomCommand(
    IUnitOfWork unitOfWork,
    RoomValidatorAsync roomValidator,
    IRoomBookSynchronizer roomBookSynchronizer,
    IAuditService auditService,
    ILogger<UpdateRoomCommand> logger)
    : ICreateOrUpdateCommand<UpdateRoomRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var roomRepo = unitOfWork.GetRepository<Room>();

        var room = (await roomRepo.GetAsync(
            r => r.Id == request.Id,
            cancellationToken: cancellationToken,
            includePaths: IncludePaths.Room.RoomBooksBook)).FirstOrDefault()
            ?? throw new RoomNotFoundException();

        // Валидация имени и проверка уникальности
        await ValidateRoomNameAsync(request.Name, cancellationToken);
        if (room.Name != request.Name && await IsNameTakenAsync(request.Name, cancellationToken))
            throw new RoomExistsException(request.Name);

        var oldName = room.Name;
        room.Name = request.Name;

        var diffLines = new List<string>();
        if (oldName != room.Name)
            diffLines.Add($"Переименована: «{oldName}» → «{room.Name}»");

        var bookRepo = unitOfWork.GetRepository<Book>();

        // Удалённые книги (были в комнате, но отсутствуют в запросе)
        var removed = room.RoomBooks.Where(rb => request.RoomBookDTO.All(dto => dto.Id != rb.Id.Value)).ToList();
        foreach (var rb in removed)
            diffLines.Add($"Удалена книга «{rb.Book.Title}»");

        // Добавленные книги (есть в запросе, но отсутствовали в комнате)
        var addedDtos = request.RoomBookDTO.Where(dto => room.RoomBooks.All(rb => rb.Id.Value != dto.Id)).ToList();
        if (addedDtos.Count > 0)
        {
            var addedBookGuids = addedDtos.Select(d => d.BookId).ToList();
            var addedBooks = await bookRepo.GetAsync(b => addedBookGuids.Contains(b.Id.Value), cancellationToken);
            var bookDict = addedBooks.ToDictionary(b => b.Id.Value);

            foreach (var dto in addedDtos)
            {
                var title = bookDict.TryGetValue(dto.BookId, out var b) ? b.Title : $"[{dto.BookId}]";
                diffLines.Add($"Добавлена книга «{title}»: {dto.BookCount} экз.");
            }
        }

        // Изменённые количества (книга осталась в комнате, но изменилось BookCount)
        var updated = room.RoomBooks
            .Where(rb => request.RoomBookDTO.Any(dto => dto.Id == rb.Id.Value && dto.BookCount != rb.BookCount))
            .ToList();
        foreach (var rb in updated)
        {
            var dto = request.RoomBookDTO.First(d => d.Id == rb.Id.Value);
            diffLines.Add($"Изменено количество «{rb.Book.Title}»: {rb.BookCount} → {dto.BookCount}");
        }

        // Транзакция охватывает синхронизацию и сохранение
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await roomBookSynchronizer.SynchronizeAsync(room, request.RoomBookDTO, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var details = diffLines.Count > 0
                ? string.Join("\n", diffLines)
                : "Обновлена комната (без изменений)";

            await auditService.LogAsync(
                AuditActionType.Update,
                nameof(Room),
                room.Id.Value,
                details: details,
                ct: cancellationToken);
        }, cancellationToken);

        return ResponseFactory.Updated<Room>();
    }
    private async Task ValidateRoomNameAsync(string name, CancellationToken ct)
    {
        var tempRoom = new Room { Name = name };
        var result = await roomValidator.ValidateAsync(tempRoom, ct);
        if (!result.IsValid)
            throw new LibValidationException { ExceptionDetails = result.Errors };
    }

    private async Task<bool> IsNameTakenAsync(string name, CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<Room>();
        return await repo.AnyAsync(r => r.Name == name, ct);
    }
}